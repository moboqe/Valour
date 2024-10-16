﻿using Valour.Shared.Models;

namespace Valour.Sdk.Models
{
    public interface IPlanetModel : ISharedModel
    {
        public long PlanetId { get; set; }

        /// <summary>
        /// Returns the planet for this model
        /// </summary>
        public static ValueTask<Planet> GetPlanetAsync(IPlanetModel model, bool refresh = false) =>
            Planet.FindAsync(model.PlanetId, refresh);
    }
}

