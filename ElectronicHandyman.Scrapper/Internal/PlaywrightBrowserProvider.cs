using Microsoft.Playwright;

namespace ElectronicHandyman.Scrapper.Internal;

public class PlaywrightBrowserProvider : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    public async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser != null)
        {
            return _browser;
        }
        
        await _semaphore.WaitAsync();
        try
        {
            if (_browser != null)
            {
                return _browser;
            }
            
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions 
            { 
                Headless = true 
            });
            
            return _browser;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _semaphore.Dispose();
    }
}