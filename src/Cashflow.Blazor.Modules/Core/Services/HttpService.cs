using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Cashflow.Blazor.Modules.Core.Services;

/// <summary>
/// A base dos clientes de cada módulo, como o <c>HttpService</c> da referência: anexa o bearer do
/// <see cref="ITokenManager"/> e traduz o ProblemDetails em <see cref="ApiException"/>. O bearer vai
/// na mensagem, não no <c>DefaultRequestHeaders</c> do cliente, que é compartilhado entre chamadas.
/// </summary>
public abstract class HttpService(HttpClient httpClient, ITokenManager tokenManager)
{
    protected async Task<T> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        using var request = await CriarAsync(HttpMethod.Get, url, cancellationToken);
        return await EnviarAsync<T>(request, cancellationToken);
    }

    /// <summary>Todo POST da API é idempotente pela chave: quem chama decide quando ela muda.</summary>
    protected async Task<T> PostAsync<T>(string url, object? data, string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        using var request = await CriarAsync(HttpMethod.Post, url, cancellationToken);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        if (data is not null)
            request.Content = JsonContent.Create(data);
        return await EnviarAsync<T>(request, cancellationToken);
    }

    private async Task<HttpRequestMessage> CriarAsync(HttpMethod method, string url, CancellationToken cancellationToken)
    {
        var token = await tokenManager.GetTokenAsync(cancellationToken);
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private async Task<T> EnviarAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw ApiException.Indisponivel();
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw ApiException.Indisponivel();
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw await ErroAsync(response, cancellationToken);

            try
            {
                return (await response.Content.ReadFromJsonAsync<T>(cancellationToken))!;
            }
            catch (System.Text.Json.JsonException)
            {
                // contrato quebrado do outro lado: a tela mostra o erro, o circuito não cai
                throw new ApiException(System.Net.HttpStatusCode.BadGateway, "resposta inválida do serviço");
            }
        }
    }

    private static async Task<ApiException> ErroAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ProblemDetails? problem = null;
        try
        {
            if (response.Content.Headers.ContentLength != 0)
                problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        }
        catch (System.Text.Json.JsonException)
        {
            // corpo que não é ProblemDetails: fica o status
        }

        var titulo = problem?.Title ?? (int)response.StatusCode switch
        {
            401 => "sessão expirada",
            403 => "sem permissão para esta operação",
            >= 500 => "erro no servidor",
            _ => "requisição recusada"
        };
        return new ApiException(response.StatusCode, titulo, problem?.Detail);
    }
}
