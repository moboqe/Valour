using Valour.Shared.Items.Messages;

namespace Valour.Server.Models;

public class PlanetMessage : Item, ISharedPlanetMessage
{
    /// <summary>
    /// The id of the planet this message belongs to
    /// </summary>
    public long PlanetId { get; set; }
    
    /// <summary>
    /// The message (if any) this is a reply to
    /// </summary>
    public long? ReplyToId { get; set; }

    /// <summary>
    /// The author's user ID
    /// </summary>
    public long AuthorUserId { get; set; }

    /// <summary>
    /// The author's member ID
    /// </summary>
    public long AuthorMemberId { get; set; }

    /// <summary>
    /// String representation of message
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// The time the message was sent (in UTC)
    /// </summary>
    public DateTime TimeSent { get; set; }

    /// <summary>
    /// Id of the channel this message belonged to
    /// </summary>
    public long ChannelId { get; set; }
    
    /// <summary>
    /// Data for representing an embed
    /// </summary>
    public string EmbedData { get; set; }

    /// <summary>
    /// Data for representing mentions in a message
    /// </summary>
    public string MentionsData { get; set; }

    /// <summary>
    /// Data for representing attachments in a message
    /// </summary>
    public string AttachmentsData { get; set; }

    /// <summary>
    /// True if the message was edited
    /// </summary>
    public bool Edited { get; set; }

    /// <summary>
    /// Used to identify a message returned from the server 
    /// </summary>
    public string Fingerprint { get; set; }
}