namespace PCP_Shooting.Domain.Models;

public sealed class OperacionException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}