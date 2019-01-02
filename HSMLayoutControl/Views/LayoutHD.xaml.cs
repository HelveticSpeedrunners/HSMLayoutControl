using System;
using System.Windows;

namespace HSMLayoutManager.Views
{
  public partial class LayoutHD : Window
  {
    public LayoutHD()
    {
      InitializeComponent();
    }

    public void SetLocalTime(int hours, int minutes)
    {
      lblCurrentTime.Content = String.Format("{0:D2}:{1:D2}", hours, minutes);
    }

    public void SetRunTime(int hours, int minutes, int seconds)
    {
      lblTimer.Content = String.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
    }

    public void SetCurrentGame(string name, string category, string estimate, string runners)
    {
      tbkGame.Text = name;
      tbkCategory.Text = category;
      lblEstimate.Content = String.Format("EST {0}", estimate);
      lblRunner.Content = runners;
    }

    public void SetLastGame(string gameName, string category, string finalTime)
    {
      lblLastGame.Content = gameName;
      lblLastResult.Content = String.Format("{0} in {1}", category, finalTime);
    }

    public void SetNextGame(string gameName, string category, string scheduleTime, string runners)
    {
      lblNextCategory.Content = category;
      lblNextGame.Content = gameName;
      lblNextRunners.Content = runners;
      lblNextScheduleTime.Content = scheduleTime;
    }

    #region Window Control

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      e.Cancel = true;
      Hide();
    }

    new public void Close()
    {
      Closing -= Window_Closing;
      base.Close();
    }

    #endregion Window Control
  }
}
