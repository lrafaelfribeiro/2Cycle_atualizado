namespace API.DTOs
{
    public sealed record EmailRequest
    (
        string To,
        string From,
        string Subject,
        string Body,
        bool IsHtml = true
    );
}
