namespace LittleEcommerce.Source.Common.Enums;

public enum OutboxStatus
{
    Pending,
    Processing,
    Sent,
    DeadLetter
}
