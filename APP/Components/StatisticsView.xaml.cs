using APP.Models;

namespace APP.Components;

public partial class StatisticsView : ContentView
{
	public static readonly BindableProperty WeeklyStatsProperty =
		BindableProperty.Create(nameof(WeeklyStats), typeof(WeeklyStatsDTO), typeof(StatisticsView));

	public WeeklyStatsDTO? WeeklyStats
	{
		get => (WeeklyStatsDTO?)GetValue(WeeklyStatsProperty);
		set => SetValue(WeeklyStatsProperty, value);	
	}
	public StatisticsView()
	{
		InitializeComponent();
	}
}