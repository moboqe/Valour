﻿using Valour.Shared.Items.Planets.Members;

namespace Valour.Api.Items.Planets.Members;

public class PlanetRoleMember : ISharedPlanetRoleMember
{
    public ulong Id { get; set; }
    public string Node { get; set; }

    public ulong UserId { get; set; }
    public ulong RoleId { get; set; }
    public ulong PlanetId { get; set; }
    public ulong MemberId { get; set; }
}

