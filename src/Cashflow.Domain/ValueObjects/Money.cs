namespace Cashflow.Domain.ValueObjects;

public sealed record Money(decimal Valor)
{
    public static Money De(decimal valor)
    {
        if (valor < 0)
            throw new ArgumentOutOfRangeException(nameof(valor), "valor não pode ser negativo");
        return new Money(valor);
    }

    public Money Negativo() => new(-Valor);

    public static Money operator +(Money a, Money b) => new(a.Valor + b.Valor);
}
