using System.Collections.Generic;
using UnityEngine;

namespace AshfallCamp.Infrastructure
{
    [CreateAssetMenu(menuName = "Ashfall Camp/Configs/Background Catalog")]
    public sealed class BackgroundCatalogSO : ScriptableObject
    {
        public List<BackgroundConfigData> Backgrounds = new List<BackgroundConfigData>();
    }
}
