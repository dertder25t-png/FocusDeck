using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FocusDeck.Mobile.Messages;

public class ForcedLogoutMessage : ValueChangedMessage<string>
{
    public ForcedLogoutMessage(string message) : base(message)
    {
    }
}
