using System;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace HavenSoft.HexManiac.AvaloniaUI.Controls;

/// <summary>Image-only hover behavior for Go To tokens; text tips keep their existing service.</summary>
public sealed class ImageHoverPreview {
   public static readonly AttachedProperty<object> ContentProperty =
      AvaloniaProperty.RegisterAttached<ImageHoverPreview, Control, object>("Content");
   private static readonly AttachedProperty<State> StateProperty =
      AvaloniaProperty.RegisterAttached<ImageHoverPreview, Control, State>("State");

   static ImageHoverPreview() {
      ContentProperty.Changed.AddClassHandler<Control>((owner, _) => {
         var state = owner.GetValue(StateProperty);
         if (state == null) owner.SetValue(StateProperty, state = new State(owner));
         state.ContentChanged();
      });
   }
   public static object GetContent(Control control) => control.GetValue(ContentProperty);
   public static void SetContent(Control control, object value) => control.SetValue(ContentProperty, value);

   private sealed class State {
      private readonly Control owner;
      private ImagePreviewLayer layer;
      private INotifyCollectionChanged collection;
      public State(Control owner) {
         this.owner = owner;
         owner.PointerEntered += Show;
         owner.PointerMoved += Show;
         owner.PointerExited += (_, _) => Hide();
         owner.AttachedToVisualTree += (_, _) => ContentChanged();
         owner.DetachedFromVisualTree += (_, _) => {
            Hide();
            if (collection != null) collection.CollectionChanged -= CollectionChanged;
            collection = null;
         };
      }
      public void ContentChanged() {
         Hide();
         if (collection != null) collection.CollectionChanged -= CollectionChanged;
         collection = GetContent(owner) as INotifyCollectionChanged;
         if (collection != null) collection.CollectionChanged += CollectionChanged;
      }
      private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => Hide();
      private void Hide() { layer?.Hide(owner); layer = null; }
      private void Show(object sender, PointerEventArgs e) {
         if (layer?.IsShowing(owner) == true) return;
         if (!ImagePreviewLayer.HasImages(GetContent(owner))) return;
         layer = ImagePreviewLayer.For(owner);
         layer?.Show(owner, GetContent(owner));
      }
   }
}
