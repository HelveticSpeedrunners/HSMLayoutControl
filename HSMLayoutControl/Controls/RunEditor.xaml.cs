using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace HSMLayoutManager.Controls
{
  /// <summary>
  /// Interaction logic for RunEditor.xaml
  /// </summary>
  public partial class RunEditor : UserControl
  {
    private bool _isHidden = false;

    public EventHandler OnRemove;
    public EventHandler OnMoveDown;

    public string GameName => txtGameName.Text.Trim();
    public string GameNameShort => txtShortGameName.Text.Trim();
    public string GameCategory => txtGameCategory.Text.Trim();
    public string GameYear => txtGameYear.Text.Trim();
    public string Platform => cmbPlatform.SelectedValue?.ToString().Trim();
    public string Runners => txtRunners.Text.Trim();
    public string Estimate => txtEstimate.Text.Trim();
    public string ScheduleTime => txtScheduleTime.Text.Trim();
    public string FinalTime => txtFinalTime.Text.Trim();

    public RunEditor() => InitializeComponent();

    public void MarkAsCurrentRun()
    {
      btnRemove.IsEnabled = false;
      btnDown.IsEnabled = false;
      txtEstimate.IsEnabled = true;
      txtGameYear.IsEnabled = true;
      txtRunners.IsEnabled = true;
      txtScheduleTime.IsEnabled = true;
      cmbPlatform.IsEnabled = true;

      Background = (new BrushConverter()).ConvertFromString("#FFDE7F7F") as Brush;
      if (_isHidden) BtnHide_Click(null, null);
    }

    public void MarkAsOpen()
    {
      btnDown.IsEnabled = true;
      btnRemove.IsEnabled = true;
      Background = (new BrushConverter()).ConvertFromString("#FFFFFFFF") as Brush;
    }

    public void SuggestFinalTime(string time)
    {
      txtFinalTime.Text = time;
    }

    public void MarkAsDone()
    {
      txtEstimate.IsEnabled = false;
      txtGameYear.IsEnabled = false;
      txtRunners.IsEnabled = false;
      txtScheduleTime.IsEnabled = false;
      cmbPlatform.IsEnabled = false;

      Background = (new BrushConverter()).ConvertFromString("#FFD6D6D6") as Brush;
      Hide();
    }

    public void ImportConfig(XmlNode run)
    {
      txtGameName.Text = run?["game"]?.InnerText;
      txtGameCategory.Text = run?["category"]?.InnerText;
      txtGameYear.Text = run?["year"]?.InnerText;
      txtRunners.Text = run?["runners"]?.InnerText;
      txtEstimate.Text = run?["estimate"]?.InnerText;
      txtScheduleTime.Text = run?["schedule"]?.InnerText;
      txtFinalTime.Text = run?["time"]?.InnerText;
      txtShortGameName.Text = run?["shortgame"]?.InnerText;

      foreach (var item in cmbPlatform.Items)
      {
        if (item is ComboBoxItem ci && (ci.Content as string)?.Equals(run?["platform"]?.InnerText) == true)
          cmbPlatform.SelectedItem = ci;
      }

      UpdateTitle(null, null);
    }

    private void BtnHide_Click(object sender, RoutedEventArgs e)
    {
      if (_isHidden) UnHide();
      else Hide();
    }

    public void Hide()
    {
      _isHidden = true;
      MaxHeight = grdHeader.ActualHeight + Padding.Top + Padding.Bottom + Margin.Top + Margin.Bottom + grdHeader.Margin.Top + grdHeader.Margin.Bottom;
      btnHide.Content = "Show";
    }

    public void UnHide()
    {
      _isHidden = false;
      MaxHeight = 1000;
      btnHide.Content = "Hide";
    }

    private void BtnRemove_Click(object sender, RoutedEventArgs e) => OnRemove?.Invoke(this, null);
    private void BtnDown_Click(object sender, RoutedEventArgs e) => OnMoveDown?.Invoke(this, null);
    private void UpdateTitle(object sender, KeyEventArgs e) => lblTitle.Content = String.Format("{0}, {1}", txtGameName.Text, txtGameCategory.Text);
  }
}
