using Client.Data;
using Client.Main.Content;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Client.Main.Objects.Wings;
using System.Threading.Tasks;
using Client.Main.Objects.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Extensions.Logging;
using System;

namespace Client.Main.Objects.NPCS
{
    [NpcInfo(257, "Elf Soldier")]
    public class ElfSoldier : NPCObject
    {
        private new readonly ILogger<ElfSoldier> _logger;
        private WingObject _wings;
        private DateTime _lastClickTime = DateTime.MinValue;
        private const double CLICK_COOLDOWN_SECONDS = 1.0;
        public ElfSoldier()
        {
            _logger = AppLoggerFactory?.CreateLogger<ElfSoldier>();

            _wings = new WingObject
            {
                BlendMesh = 0,
                BlendMeshState = Microsoft.Xna.Framework.Graphics.BlendState.Additive
            };
            Children.Add(_wings);
        }

        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare("Player/Player.bmd");
            if (Model == null)
            {
                _logger.LogError("CRITICAL: Could not load base player model 'Player/Player.bmd'. NPC cannot be animated.");
                Status = GameControlStatus.Error;
                return;
            }

            await SetBodyPartsAsync("Player/", "HelmMale", "ArmorMale", "PantMale", "GloveMale", "BootMale", 25);

            // Set item enhancement level +11 for all equipment parts
            Helm.ItemLevel = 11;
            Armor.ItemLevel = 11;
            Pants.ItemLevel = 11;
            Gloves.ItemLevel = 11;
            Boots.ItemLevel = 11;

            _wings.Model = await BMDLoader.Instance.Prepare("Item/Wing04.bmd");

            await base.Load();

            CurrentAction = (int)PlayerAction.PlayerStopFly;
            Scale = 1.0f;

            var currentBBox = BoundingBoxLocal;
            BoundingBoxLocal = new BoundingBox(currentBBox.Min,
                new Vector3(currentBBox.Max.X, currentBBox.Max.Y, currentBBox.Max.Z + 70f));
        }

        protected override void HandleClick()
        {
            // Debounce clicks - only allow one request per second
            var now = DateTime.UtcNow;
            var timeSinceLastClick = (now - _lastClickTime).TotalSeconds;

            if (timeSinceLastClick < CLICK_COOLDOWN_SECONDS)
            {
                _logger?.LogDebug("Click ignored - cooldown active ({TimeRemaining:F2}s remaining)",
                    CLICK_COOLDOWN_SECONDS - timeSinceLastClick);
                return;
            }

            _lastClickTime = now;
            _logger?.LogInformation("Elf Soldier clicked - sending buff request sequence (NetworkId: {NetworkId})", NetworkId);

            // Send complete buff sequence: TalkToNpc -> BuffRequest
            var characterService = MuGame.Network?.GetCharacterService();
            if (characterService != null)
            {
                _ = characterService.SendElfSoldierBuffSequenceAsync(NetworkId);
            }
            else
            {
                _logger?.LogWarning("CharacterService is null - cannot send Elf Soldier buff sequence");
            }
        }
    }
}