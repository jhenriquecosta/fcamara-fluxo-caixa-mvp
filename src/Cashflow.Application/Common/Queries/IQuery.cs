namespace Cashflow.Application;

/// <summary>Um caso de uso só de leitura (CQRS, lado da consulta), que devolve <typeparamref name="TResult"/>.</summary>
/// <typeparam name="TResult">O modelo devolvido a quem consultou.</typeparam>
public interface IQuery<TResult>;
