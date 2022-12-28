﻿using System.ComponentModel.DataAnnotations.Schema;
using Valour.Shared.Items.Planets.Members;

namespace Valour.Database;

[Table("planet_roles")]
public class PlanetRole : Item, ISharedPlanetRole
{
    [ForeignKey("PlanetId")]
    public Planet Planet { get; set; }
    
    [InverseProperty("Role")]
    public virtual ICollection<PermissionsNode> PermissionNodes { get; set; }

    /// <summary>
    /// The id of the planet this belongs to
    /// </summary>
    [Column("planet_id")]
    public long PlanetId { get; set; }

    /// <summary>
    /// The position of the role: Lower has more authority
    /// </summary>
    [Column("position")]
    public int Position { get; set; }

    /// <summary>
    /// The planet permissions for the role
    /// </summary>
    [Column("permissions")]
    public long Permissions { get; set; }

    // RGB Components for role color
    [Column("red")]
    public byte Red { get; set; }

    [Column("green")]
    public byte Green { get; set; }

    [Column("blue")]
    public byte Blue { get; set; }

    // Formatting options
    [Column("bold")]
    public bool Bold { get; set; }

    [Column("italics")]
    public bool Italics { get; set; }

    [Column("name")]
    public string Name { get; set; }
}