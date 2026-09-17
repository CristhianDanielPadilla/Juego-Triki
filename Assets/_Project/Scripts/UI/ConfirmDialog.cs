using System;
using UnityEngine.UIElements;

namespace Triki.UI
{
    /// <summary>
    /// Ventana modal de confirmación. "Cancelar" recibe el foco al abrir y Escape también cancela,
    /// para que una acción destructiva nunca ocurra por accidente.
    /// </summary>
    internal sealed class ConfirmDialog
    {
        private const string HiddenClass = "hidden";

        private readonly VisualElement _overlay;
        private readonly Label _title;
        private readonly Label _message;
        private readonly Button _accept;
        private readonly Button _cancel;
        private Action _onConfirm;

        public ConfirmDialog(VisualElement overlay)
        {
            _overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
            _title = overlay.Q<Label>("confirm-title");
            _message = overlay.Q<Label>("confirm-message");
            _accept = overlay.Q<Button>("confirm-accept");
            _cancel = overlay.Q<Button>("confirm-cancel");
        }

        public bool IsOpen => !_overlay.ClassListContains(HiddenClass);

        public void Bind()
        {
            _accept.clicked += Confirm;
            _cancel.clicked += Cancel;
            _overlay.RegisterCallback<NavigationCancelEvent>(HandleNavigationCancel);
        }

        public void Unbind()
        {
            _accept.clicked -= Confirm;
            _cancel.clicked -= Cancel;
            _overlay.UnregisterCallback<NavigationCancelEvent>(HandleNavigationCancel);
        }

        public void Show(string title, string message, string confirmText, Action onConfirm)
        {
            _onConfirm = onConfirm ?? throw new ArgumentNullException(nameof(onConfirm));
            _title.text = title;
            _message.text = message;
            _accept.text = confirmText;
            _overlay.RemoveFromClassList(HiddenClass);
            _cancel.Focus();
        }

        /// <summary>Ejecuta la acción y cierra. Lo llama el botón de aceptar.</summary>
        internal void Confirm()
        {
            if (!IsOpen)
                return;

            var action = _onConfirm;
            Close();
            action?.Invoke();
        }

        /// <summary>Cierra sin hacer nada. Lo llaman el botón de cancelar y Escape.</summary>
        internal void Cancel()
        {
            if (IsOpen)
                Close();
        }

        private void HandleNavigationCancel(NavigationCancelEvent evt)
        {
            Cancel();
            evt.StopPropagation();
        }

        private void Close()
        {
            _onConfirm = null;
            _overlay.AddToClassList(HiddenClass);
        }
    }
}
