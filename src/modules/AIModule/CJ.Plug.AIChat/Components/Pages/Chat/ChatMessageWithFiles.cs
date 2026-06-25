using Microsoft.Extensions.AI;
using System.Collections.Generic;

namespace CJ.Plug.AIChat.Components.Pages.Chat;

public record FileAttachmentInfo(string FileId, string FileName);

public record ChatMessageWithFiles(ChatMessage Message, IReadOnlyList<FileAttachmentInfo> Files)
{
    public bool HasFiles => Files.Count > 0;
}
