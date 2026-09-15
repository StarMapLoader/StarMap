using Brutal.ImGuiApi;
using Brutal.Numerics;
using KSA;

namespace StarMap.Core.UI.ConfirmRestart
{
    internal class ConfirmRestartPopup : Popup
    {
        private static readonly float2 SIZE_UV = new float2(0.32f, 0.213333f);

        private readonly string _title;
        public ConfirmRestart UI { get; }
        private static readonly PopupButton<ConfirmRestartPopup> PopupButtonContinue = CreateButton("Continue", (Action<ConfirmRestartPopup>)(popup =>
        {
            popup.Active = false;
            popup.UI.Restart = false;
            popup.UI.Show = false;
        }));
        private static readonly PopupButton<ConfirmRestartPopup> PopupButtonRestart = CreateButton("Restart", (Action<ConfirmRestartPopup>)(popup =>
        {
            popup.Active = false;
            popup.UI.Restart = true;
            popup.UI.Show = false;
        }));

        private static readonly PopupButton<ConfirmRestartPopup>[] ButtonMatrix = [
            PopupButtonContinue,
            PopupButtonRestart
        ];

        private ConfirmRestartPopup(ConfirmRestart ui)
        { 
            _title = "Game requires restart";
            UI = ui;
        }

        public static ConfirmRestartPopup Create(ConfirmRestart ui) => new(ui);

        protected override void OnDrawUi()
        {
            if (!BeginConsoleModal(WindowId.AsSpan(), _title.AsSpan(), SIZE_UV))
                return;
            ConsoleStyle.BeginBody();
            ConsoleStyle.PushWidgetStyle();
            CenteredWrappedText("New mods have been enabled after starting KSA.\nThe game needs to be restarted for these mods to be loaded in StarMap.".AsSpan());
            EndConsoleBody();
            DrawConsoleButtonRow<ConfirmRestartPopup>(this, ButtonMatrix);
            EndConsoleModal();
        }
    }
}
