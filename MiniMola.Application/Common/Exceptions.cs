using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.Common.Exceptions;

/// <summary>
/// Kullanıcıya kontrollü şekilde dönülebilecek
/// uygulama hatalarının temel sınıfıdır.
/// </summary>
public abstract class MiniMolaException : Exception
{
    protected MiniMolaException(string message)
        : base(message)
    {
    }

    protected MiniMolaException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// İstenen kayıt veya kaynak bulunamadığında kullanılır.
/// HTTP karşılığı: 404 Not Found.
/// </summary>
public sealed class NotFoundException : MiniMolaException
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// İş kuralı nedeniyle işlem tamamlanamadığında kullanılır.
/// Örneğin yetersiz Damla veya dolu akvaryum.
/// HTTP karşılığı: 409 Conflict.
/// </summary>
public sealed class ConflictException : MiniMolaException
{
    public ConflictException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Spotify, haber veya ileride AI gibi dış servisler
/// kullanılamadığında kullanılır.
/// HTTP karşılığı: 503 Service Unavailable.
/// </summary>
public sealed class ExternalServiceException : MiniMolaException
{
    public ExternalServiceException(string message)
        : base(message)
    {
    }

    public ExternalServiceException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}