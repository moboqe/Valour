﻿using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Valour.Database.Items.Planets.Members;
using Valour.Shared.Items;
using Valour.Shared.Items.Users;

namespace Valour.Database.Items.Users;

public class User : Item, ISharedUser
{
    [InverseProperty("User")]
    [JsonIgnore]
    public virtual UserEmail Email { get; set; }

    [InverseProperty("User")]
    [JsonIgnore]
    public virtual ICollection<PlanetMember> Membership { get; set; }

    /// <summary>
    /// The url for the user's profile picture
    /// </summary>
    public string PfpUrl { get; set; }

    /// <summary>
    /// The Date and Time that the user joined Valour
    /// </summary>
    public DateTime Joined { get; set; }

    /// <summary>
    /// The name of this user
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// True if the user is a bot
    /// </summary>
    public bool Bot { get; set; }

    /// <summary>
    /// True if the account has been disabled
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// True if this user is a member of the Valour official staff team. Falsely modifying this 
    /// through a client modification to present non-official staff as staff is a breach of our
    /// license. Don't do that.
    /// </summary>
    public bool ValourStaff { get; set; }

    /// <summary>
    /// The user's currently set status - this could represent how they feel, their disdain for the political climate
    /// of the modern world, their love for their mother's cooking, or their hate for lazy programmers.
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// The integer representation of the current user state
    /// </summary>
    public int UserStateCode { get; set; }

    /// <summary>
    /// The last time this user was flagged as active (successful auth)
    /// </summary>
    public DateTime LastActive { get; set; }

    public override ItemType ItemType => ItemType.User;

    /// <summary>
    /// The span of time from which the user was last active
    /// </summary>
    public TimeSpan LastActiveSpan =>
        ISharedUser.GetLastActiveSpan(this);

    /// <summary>
    /// The current activity state of the user
    /// </summary>
    public UserState UserState
    {
        get => ISharedUser.GetUserState(this);
        set => ISharedUser.SetUserState(this, value);
    }
}

