﻿using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Valour.Database.Items.Planets.Members;
using Valour.Shared.Authorization;
using Valour.Shared.Items;
using Valour.Shared.Items.Planets.Channels;

namespace Valour.Database.Items.Planets.Channels;

public interface IPlanetChannel
{
    [JsonIgnore]
    [ForeignKey("Planet_Id")]
    public Planet Planet { get; set; }

    [JsonIgnore]
    [ForeignKey("Parent_Id")]
    public PlanetCategory Parent { get; set; }

    [JsonInclude]
    [JsonPropertyName("Id")]
    public ulong Id { get; set; }

    [JsonInclude]
    [JsonPropertyName("Position")]
    public ushort Position { get; set; }

    [JsonInclude]
    [JsonPropertyName("Parent_Id")]
    public ulong? Parent_Id { get; set; }

    [JsonInclude]
    [JsonPropertyName("Planet_Id")]
    public ulong Planet_Id { get; set; }

    [JsonInclude]
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonInclude]
    [JsonPropertyName("Description")]
    public string Description { get; set; }

    public ItemType ItemType { get; }

    public static async Task<IPlanetChannel> FindAsync(ItemType type, ulong id, ValourDB db)
    {
        switch (type)
        {
            case ItemType.ChatChannel:
                return await PlanetChatChannel.FindAsync(id, db);
            case ItemType.Category:
                return await PlanetCategory.FindAsync(id, db);
            default:
                throw new ArgumentOutOfRangeException(nameof(ItemType));
        }
    }

    public async Task<Planet> GetPlanetAsync(ValourDB db)
    {
        Planet ??= await Planet.FindAsync(Planet_Id, db);
        return Planet;
    }

    /// <summary>
    /// Returns the parent category of this channel
    /// </summary>
    public async Task<PlanetCategory> GetParentAsync(ValourDB db)
    {
        Parent ??= await db.PlanetCategories.FindAsync(Parent_Id);
        return Parent;
    }

    public abstract void NotifyClientsChange();

    public abstract Task<bool> HasPermission(PlanetMember member, Permission permission, ValourDB db);
}

