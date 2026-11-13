namespace SpacePlantingBrick
{
    /// <summary>
    /// Environment button in the action area below the building details panel.
    /// It cycles through Fully Sealed, Gas Permeable, and Gas + Liquid
    /// Permeable.
    /// </summary>
    public sealed class SpacePlantingBrickEnvironmentButton : KMonoBehaviour
    {
        private SpacePlantingBrick brick;

        // IntraObjectHandler would invoke every button of this type on the
        // object; this subscription carries the component instance so each
        // button is refreshed exactly once.
        private static readonly System.Action<object, object> OnRefreshUserMenuAction =
            delegate (object handlerData, object data)
            {
                ((SpacePlantingBrickEnvironmentButton)handlerData).OnRefreshUserMenu(data);
            };

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Subscribe(493375141, OnRefreshUserMenuAction, this);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            brick = GetComponent<SpacePlantingBrick>();
            RefreshUserMenu();
        }

        private void OnRefreshUserMenu(object data)
        {
            if (brick == null)
            {
                brick = GetComponent<SpacePlantingBrick>();
            }
            if (brick == null || Game.Instance == null || Game.Instance.userMenu == null)
            {
                return;
            }

            KIconButtonMenu.ButtonInfo button = new KIconButtonMenu.ButtonInfo(
                "action_control",
                ButtonText,
                OnPressed,
                Action.NumActions,
                null,
                null,
                null,
                ButtonTooltip,
                true);
            Game.Instance.userMenu.AddButton(gameObject, button, 2f);
        }

        private string ButtonText
        {
            get
            {
                string state = STRINGS.UI.SPACEPLANTINGBRICK.SEALED;
                if (brick != null)
                {
                    switch (brick.EnvironmentMode)
                    {
                        case SpacePlantingEnvironmentMode.GasOnly:
                            state = STRINGS.UI.SPACEPLANTINGBRICK.GAS_PERMEABLE;
                            break;
                        case SpacePlantingEnvironmentMode.GasLiquid:
                            state = STRINGS.UI.SPACEPLANTINGBRICK.PERMEABLE;
                            break;
                    }
                }
                return state;
            }
        }

        private string ButtonTooltip
        {
            get
            {
                return STRINGS.UI.SPACEPLANTINGBRICK.ENV_TOOLTIP;
            }
        }

        private void OnPressed()
        {
            if (brick != null)
            {
                brick.CycleEnvironmentMode();
            }
        }

        private void RefreshUserMenu()
        {
            if (Game.Instance != null && Game.Instance.userMenu != null)
            {
                Game.Instance.userMenu.Refresh(gameObject);
            }
        }
    }
}
