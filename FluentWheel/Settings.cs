using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Newtonsoft.Json;

namespace Cloris.FluentWheel;

internal class Settings(WritableSettingsStore store)
{
    private const string CollectionPath = nameof(FluentWheel);

    private bool? _isHorizontalScrollingEnabled;
    private int? _horizontalScrollRate;
    private int? _scrollDuration;
    private int? _verticalScrollRate;

    private bool? _isZoomingEnabled;
    private int? _zoomDuration;

    public static Settings Current { get; private set; }

    public bool IsHorizontalScrollingEnabled
    {
        get => _isHorizontalScrollingEnabled ??= Get(false);
        set
        {
            if (_isHorizontalScrollingEnabled != value)
            {
                _isHorizontalScrollingEnabled = value;
                Set(value);
            }
        }
    }

    public int HorizontalScrollRate
    {
        get => _horizontalScrollRate ??= Get(100);
        set
        {
            if (value is < -400)
                value = -400;
            if (value is > 400)
                value = 400;

            if (_horizontalScrollRate != value)
            {
                _horizontalScrollRate = value;
                Set(value);
            }
        }
    }

    public int ScrollDuration
    {
        get => _scrollDuration ??= Get(100);
        set
        {
            if (value is < 0)
                value = 0;
            if (value is > 1000)
                value = 1000;

            if (_scrollDuration != value)
            {
                _scrollDuration = value;
                Set(value);
            }
        }
    }

    public int VerticalScrollRate
    {
        get => _verticalScrollRate ??= Get(100);
        set
        {
            if (value is < -400)
                value = -400;
            if (value is > 400)
                value = 400;

            if (_verticalScrollRate != value)
            {
                _verticalScrollRate = value;
                Set(value);
            }
        }
    }

    public bool IsZoomingEnabled
    {
        get => _isZoomingEnabled ??= Get(false);
        set
        {
            if (_isZoomingEnabled != value)
            {
                _isZoomingEnabled = value;
                Set(value);
            }
        }
    }

    public int ZoomDuration
    {
        get => _zoomDuration ??= Get(100);
        set
        {
            if (value is < 0)
                value = 0;
            if (value is > 1000)
                value = 1000;

            if (_zoomDuration != value)
            {
                _zoomDuration = value;
                Set(value);
            }
        }
    }

    public static async ValueTask InitializeAsync(AsyncPackage package)
    {
        await Task.Yield();

        var manager = new ShellSettingsManager(package);
        var store = manager.GetWritableSettingsStore(SettingsScope.UserSettings);

        if (!store.CollectionExists(CollectionPath))
            store.CreateCollection(CollectionPath);

        Current = new Settings(store);
    }

    private T Get<T>(T defaultValue = default, [CallerMemberName] string key = default)
    {
        if (!store.PropertyExists(CollectionPath, key))
            return defaultValue;

        return typeof(T).Name switch
        {
            nameof(Int32) => store.GetInt32(CollectionPath, key) as dynamic,
            nameof(UInt32) => store.GetUInt32(CollectionPath, key) as dynamic,
            nameof(Int64) => store.GetInt64(CollectionPath, key) as dynamic,
            nameof(UInt64) => store.GetUInt64(CollectionPath, key) as dynamic,
            nameof(Boolean) => store.GetBoolean(CollectionPath, key) as dynamic,
            nameof(String) => store.GetString(CollectionPath, key) as dynamic,
            _ => JsonConvert.DeserializeObject<T>(store.GetString(CollectionPath, key)),
        };
    }

    private void Set<T>(T value, [CallerMemberName] string key = default)
    {
        switch (value)
        {
            case null:
                store.DeleteProperty(CollectionPath, key);
                break;

            case int:
                store.SetInt32(CollectionPath, key, value as dynamic);
                break;

            case uint:
                store.SetUInt32(CollectionPath, key, value as dynamic);
                break;

            case long:
                store.SetInt64(CollectionPath, key, value as dynamic);
                break;

            case ulong:
                store.SetUInt64(CollectionPath, key, value as dynamic);
                break;

            case bool:
                store.SetBoolean(CollectionPath, key, value as dynamic);
                break;

            case string:
                store.SetString(CollectionPath, key, value as dynamic);
                break;

            default:
                store.SetString(CollectionPath, key, JsonConvert.SerializeObject(value));
                break;
        }
    }
}