using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace HavenSoft.HexManiac.AvaloniaUI.Windows;

/// <summary>Present the exported filename and location without a wall of path text.</summary>
public partial class MapImageSavedDialog : Window {
   private readonly Action reveal;

   public MapImageSavedDialog() => InitializeComponent();

   public MapImageSavedDialog(string path, Action reveal) : this() {
      this.reveal = reveal;
      FileNameText.Text = Path.GetFileName(path);
      DirectoryText.Text = Path.GetDirectoryName(path);
   }

   private void Reveal(object sender, RoutedEventArgs e) => reveal?.Invoke();
   private void Accept(object sender, RoutedEventArgs e) => Close();
}
