﻿using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Valour.Database.Items.Authorization;
using Valour.Database.Items.Planets.Members;
using Valour.Shared;
using Valour.Shared.Authorization;
using Valour.Shared.Items;
using Valour.Shared.Items.Authorization;
using Valour.Shared.Items.Planets.Channels;

namespace Valour.Database.Items.Planets.Channels;

[Table("PlanetCategoryChannels")]
public class PlanetCategoryChannel : PlanetChannel, IPlanetItem<PlanetCategoryChannel>, ISharedPlanetCategoryChannel, INodeSpecific
{

    [JsonIgnore]
    public static readonly Regex nameRegex = new Regex(@"^[a-zA-Z0-9 _-]+$");

    /// <summary>
    /// The type of this item
    /// </summary>
    [NotMapped]
    public override ItemType ItemType => ItemType.PlanetCategoryChannel;

    /// <summary>
    /// Tries to delete the category while respecting constraints
    /// </summary>
    public async Task DeleteAsync(ValourDB db)
    {
        // Remove permission nodes
        db.PermissionsNodes.RemoveRange(
            db.PermissionsNodes.Where(x => x.Target_Id == Id)
        );

        // Remove category
        db.PlanetCategories.Remove(
            await db.PlanetCategories.FindAsync(Id)
        );

        // Save changes
        await db.SaveChangesAsync();

        // Notify of update
        PlanetHub.NotifyPlanetItemDelete(this);
    }

    /// <summary>
    /// Sets the name of this category
    /// </summary>
    public async Task<TaskResult> TrySetNameAsync(string name, ValourDB db)
    {
        TaskResult validName = ValidateName(name);
        if (!validName.Success) return validName;

        this.Name = name;
        db.PlanetCategories.Update(this);
        await db.SaveChangesAsync();

        NotifyClientsChange();

        return new TaskResult(true, "Success");
    }

    /// <summary>
    /// Sets the description of this category
    /// </summary>
    public async Task SetDescriptionAsync(string desc, ValourDB db)
    {
        this.Description = desc;
        db.PlanetCategories.Update(this);
        await db.SaveChangesAsync();

        NotifyClientsChange();
    }

    /// <summary>
    /// Sets the parent of this category
    /// </summary>
    public async Task<TaskResult<int>> TrySetParentAsync(PlanetMember member, ulong? parent_id, int position, ValourDB db)
    {
        if (member == null)
            return new TaskResult<int>(false, "Member not found", 403);
        if (!await HasPermission(member, CategoryPermissions.ManageCategory, db))
            return new TaskResult<int>(false, "Member lacks CategoryPermissions.ManageCategory", 403);

        if (parent_id != null)
        {
            var parent = await db.PlanetCategories.FindAsync(parent_id);
            if (parent == null) return new TaskResult<int>(false, "Could not find parent", 404);
            if (parent.Planet_Id != Planet_Id) return new TaskResult<int>(false, "Category belongs to a different planet", 400);
            if (parent.Id == Id) return new TaskResult<int>(false, "Cannot be own parent", 400);

            if (position == -1)
            {
                var o_cats = await db.PlanetCategories.CountAsync(x => x.Parent_Id == parent_id);
                var o_chans = await db.PlanetChatChannels.CountAsync(x => x.Parent_Id == parent_id);
                this.Position = (ushort)(o_cats + o_chans);
            }
            else
            {
                this.Position = (ushort)position;
            }

            // TODO: additional loop checking
        }
        else
        {
            if (position == -1)
            {
                var o_cats = await db.PlanetCategories.CountAsync(x => x.Planet_Id == Planet_Id && x.Parent_Id == null);
                this.Position = (ushort)o_cats;
            }
            else
            {
                this.Position = (ushort)position;
            }
        }

        this.Parent_Id = parent_id;
        db.PlanetCategories.Update(this);
        await db.SaveChangesAsync();

        NotifyClientsChange();

        return new TaskResult<int>(true, "Success", 200);
    }

    /// <summary>
    /// Returns if the member has the given permission in this category
    /// </summary>
    public async Task<bool> HasPermission(PlanetMember member, Permission permission, ValourDB db)
    {
        Planet planet = await GetPlanetAsync(db);

        if (planet.Owner_Id == member.User_Id)
        {
            return true;
        }

        // If true, we ask the parent
        if (InheritsPerms)
        {
            return await (await GetParentAsync(db)).HasPermission(member, permission, db);
        }

        var roles = await member.GetRolesAsync(db);

        var do_channel = permission is ChatChannelPermission;

        // Starting from the most important role, we stop once we hit the first clear "TRUE/FALSE".
        // If we get an undecided, we continue to the next role down
        foreach (var role in roles.OrderBy(x => x.Position))
        {
            PermissionsNode node = null;

            if (do_channel)
                node = await role.GetChannelNodeAsync(this, db);
            else
                node = await role.GetCategoryNodeAsync(this, db);

            // If we are dealing with the default role and the behavior is undefined, we fall back to the default permissions
            if (node == null)
            {
                if (role.Id == planet.Default_Role_Id)
                {
                    if (do_channel)
                        return Permission.HasPermission(ChatChannelPermissions.Default, permission);
                    else
                        return Permission.HasPermission(CategoryPermissions.Default, permission);
                }

                continue;
            }

            PermissionState state = PermissionState.Undefined;

            state = node.GetPermissionState(permission);

            if (state == PermissionState.Undefined)
            {
                continue;
            }
            else if (state == PermissionState.True)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        // No roles ever defined behavior: resort to false.
        return false;
    }

    /// <summary>
    /// Validates that a given name is allowable for a server
    /// </summary>
    public static TaskResult ValidateName(string name)
    {
        if (name.Length > 32)
        {
            return new TaskResult(false, "Planet names must be 32 characters or less.");
        }

        if (!nameRegex.IsMatch(name))
        {
            return new TaskResult(false, "Planet names may only include letters, numbers, dashes, and underscores.");
        }

        return TaskResult.SuccessResult;
    }

    public async Task<TaskResult> CanGetAsync(PlanetMember member, ValourDB db)
    {
        if (member is null)
            return new TaskResult(false, "User is not a member of the target planet");

        if (!await HasPermission(member, CategoryPermissions.View, db))
            return new TaskResult(false, "Member lacks category permission " + CategoryPermissions.View.Name);

        return TaskResult.SuccessResult;
    }

    public async Task<TaskResult> CanUpdateAsync(PlanetMember member, ValourDB db)
    {
        throw new NotImplementedException();
    }

    public async Task<TaskResult> CanCreateAsync(PlanetMember member, ValourDB db)
    {
        throw new NotImplementedException();
    }

    public async Task<TaskResult> CanDeleteAsync(PlanetMember member, ValourDB db)
    {
        Planet ??= await GetPlanetAsync(db);

        if (!await Planet.HasPermissionAsync(member, PlanetPermissions.ManageCategories, db))
            return new TaskResult(false, "Member lacks planet permission " + PlanetPermissions.ManageCategories.Name);

        if (!await HasPermission(member, CategoryPermissions.ManageCategory, db))
            return new TaskResult(false, "Member lacks category permission " + CategoryPermissions.ManageCategory.Name);


        if (await db.PlanetCategories.CountAsync(x => x.Planet_Id == Planet_Id) < 2)
            return new TaskResult(false, "Last category cannot be deleted");

        var childCategoryCount = await db.PlanetCategories.CountAsync(x => x.Parent_Id == Id);
        var childChannelCount = await db.PlanetChatChannels.CountAsync(x => x.Parent_Id == Id);

        if (childCategoryCount != 0 || childChannelCount != 0)
            return new TaskResult(false, "Category must be empty");

        return new TaskResult(true, "Success");
    }

    public async Task<TaskResult> ValidateItemAsync(PlanetCategoryChannel old, ValourDB db)
    {
        throw new NotImplementedException();
    }
}

