using Brutal.ImGuiApi;
using KSA;

namespace StarMap.Core.UIElements
{
    internal class AdviceRestartPopup : Popup
    {
        private readonly string _title;
        private readonly IPopupWidget<AdviceRestartPopup>[] _widgetMatrix;
        private readonly string _text = "It was detected that new mods have been enabled during the runtime of the game, these mods will not have been loaded by StarMap. To enable these mods, please restart the game!";
        private readonly PopupToken[] _textList;

        private AdviceRestartPopup()
        {
            _title = "Please restart####" + PopupId;
            _widgetMatrix = [PopupButtonOkay];
            _textList = [.. StringTokenParser.Parse(_text)];
        }

        public static AdviceRestartPopup Create() => new AdviceRestartPopup();

        protected override void OnDrawUi()
        {
            ImGui.OpenPopup((ImString)_title);
            ImGui.BeginPopupModal((ImString)_title, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.Popup | ImGuiWindowFlags.Modal);
            ImGuiHelper.SetCurrentWindowToCenter();
            PopupToken.Draw(_textList);
            ImGui.Separator();
            DrawUi(this, _widgetMatrix);
            ImGui.EndPopup();
        }
    }
}
