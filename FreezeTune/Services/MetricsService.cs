using Prometheus;

namespace FreezeTune.Services;

public class MetricsService
{
    public Counter GamesCompleted { get; } = Metrics.CreateCounter(
        "freezetune_games_completed_total",
        "Total number of completed games",
        new CounterConfiguration
        {
            LabelNames = ["category", "result"]
        });

    public Counter GuessesTotal { get; } = Metrics.CreateCounter(
        "freezetune_guesses_total",
        "Total number of guesses made",
        new CounterConfiguration
        {
            LabelNames = ["category"]
        });

    public Histogram GuessesToWin { get; } = Metrics.CreateHistogram(
        "freezetune_guesses_to_win",
        "Distribution of guesses needed to win",
        new HistogramConfiguration
        {
            LabelNames = ["category"],
            Buckets = [1, 2, 3, 4, 5, 6, 7, 8]
        });

    public Gauge SongsAvailable { get; } = Metrics.CreateGauge(
        "freezetune_songs_available",
        "Number of songs available per category",
        new GaugeConfiguration
        {
            LabelNames = ["category"]
        });

    public Gauge SongsAvailableUntil { get; } = Metrics.CreateGauge(
        "freezetune_songs_available_until_days",
        "Days until songs run out per category (from today)",
        new GaugeConfiguration
        {
            LabelNames = ["category"]
        });

    public void RecordGameCompleted(string category, int guesses, bool success)
    {
        var result = success ? "success" : "failure";
        GamesCompleted.WithLabels(category, result).Inc();

        if (success)
        {
            GuessesToWin.WithLabels(category).Observe(guesses);
        }
    }

    public void RecordGuess(string category)
    {
        GuessesTotal.WithLabels(category).Inc();
    }

    public void UpdateCategoryStats(string category, int songCount, DateOnly? availableUntil)
    {
        SongsAvailable.WithLabels(category).Set(songCount);

        if (availableUntil.HasValue)
        {
            var daysRemaining = availableUntil.Value.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber;
            SongsAvailableUntil.WithLabels(category).Set(daysRemaining);
        }
    }
}