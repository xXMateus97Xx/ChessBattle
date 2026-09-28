using NUnit.Framework;

namespace Chess.Uci.Connector.Test;

public class SettingsTest
{
    [Test]
    public void ShouldLoadConfCorrectly()
    {
        var settings = Settings.Default;

        Assert.IsTrue(settings.Threads == 1);
        Assert.IsTrue(settings.EnginePath == "stockfish.exe");
    }

    [Test]
    public void ShouldSaveAndLoadConfCorrectly()
    {
        var settings = Settings.Load("config-test.conf");

        var threads = settings.Threads + 1;

        settings.Threads = threads;

        settings.Save();

        var newSettings = Settings.Load("config-test.conf");

        Assert.IsTrue(newSettings.Threads == threads);
    }

    [Test]
    public void ShouldThrowExceptionOnSave()
    {
        var settings = Settings.Load("config-test.conf");
        settings.Threads = -10;

        Assert.Throws<ApplicationException>(() => settings.Save());

        settings.Threads = 2;
        settings.EnginePath = "randomString";

        Assert.Throws<ApplicationException>(() => settings.Save());
    }

    [Test]
    public void ShoulThrowExceptionOnLoad()
    {
        Assert.Throws<ApplicationException>(() => Settings.Load("config-test-blank.conf"));
        Assert.Throws<ApplicationException>(() => Settings.Load("config-test-unknown.conf"));
        Assert.Throws<ApplicationException>(() => Settings.Load("conf-test-thread-exception.conf"));
        Assert.Throws<ApplicationException>(() => Settings.Load("conf-test-engine-path-exception.conf"));
    }
}