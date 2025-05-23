﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Reflection;
using System.Text.Json.Nodes;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using AdaptiveCards.Templating;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Dashboard.Common.Renderers;
using DevHome.Dashboard.Common.Services;
using DevHome.Dashboard.ComSafeWidgetObjects;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;
using Newtonsoft.Json.Linq;
using Serilog;

namespace DevHome.Dashboard.ViewModels;

/// <summary>
/// Delegate factory for creating widget view models
/// </summary>
/// <param name="widget">Widget</param>
/// <param name="widgetSize">WidgetSize</param>
/// <param name="widgetDefinition">WidgetDefinition</param>
/// <returns>Widget view model</returns>
public delegate WidgetViewModel WidgetViewModelFactory(
    ComSafeWidget widget,
    WidgetSize widgetSize,
    ComSafeWidgetDefinition widgetDefinition);

public partial class WidgetViewModel : ObservableObject, IDisposable
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetViewModel));

    public RoutedEventHandler? Loaded { get; set; }

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly IAdaptiveCardRenderingService _renderingService;

    private readonly AdaptiveElementParserRegistration _elementParser;
    private readonly AdaptiveActionParserRegistration _actionParser;

    private RenderedAdaptiveCard _renderedCard = null!;

    [ObservableProperty]
    private ComSafeWidget _widget;

    [ObservableProperty]
    private ComSafeWidgetDefinition _widgetDefinition;

    [ObservableProperty]
    private WidgetSize _widgetSize;

    [ObservableProperty]
    private bool _isCustomizable;

    [ObservableProperty]
    private string _widgetDisplayTitle = string.Empty;

    [ObservableProperty]
    private string _widgetProviderDisplayTitle = string.Empty;

    [ObservableProperty]
    private FrameworkElement _widgetFrameworkElement = null!;

    [ObservableProperty]
    private bool _isLoaded = false;

    partial void OnWidgetChanging(ComSafeWidget value)
    {
        UnsubscribeFromWidgetUpdates();
        WidgetFrameworkElement = null!;
        IsLoaded = false;
    }

    partial void OnWidgetChanged(ComSafeWidget value)
    {
        if (Widget != null)
        {
            Widget.WidgetUpdated += HandleWidgetUpdated;
            ShowWidgetContentIfAvailable();
        }
    }

    partial void OnWidgetDefinitionChanged(ComSafeWidgetDefinition value)
    {
        if (WidgetDefinition != null)
        {
            WidgetDisplayTitle = WidgetDefinition.DisplayTitle;
            WidgetProviderDisplayTitle = WidgetDefinition.ProviderDefinitionDisplayName;
            IsCustomizable = WidgetDefinition.IsCustomizable;
        }
    }

    partial void OnIsLoadedChanged(bool value)
    {
        _log.Debug((IsLoaded ? "Load" : "Unload") + $" widget {Widget.Id}");
        if (IsLoaded)
        {
            Loaded?.Invoke(this, new RoutedEventArgs());
        }
    }

    public WidgetViewModel(
        ComSafeWidget widget,
        WidgetSize widgetSize,
        ComSafeWidgetDefinition widgetDefinition,
        IAdaptiveCardRenderingService adaptiveCardRenderingService,
        DispatcherQueue dispatcherQueue)
    {
        _renderingService = adaptiveCardRenderingService;
        _dispatcherQueue = dispatcherQueue;

        Widget = widget;
        WidgetSize = widgetSize;
        WidgetDefinition = widgetDefinition;

        // Use custom parser.
        _elementParser = new AdaptiveElementParserRegistration();
        _elementParser.Set(LabelGroup.CustomTypeString, new LabelGroupParser());
        _actionParser = new AdaptiveActionParserRegistration();
        _actionParser.Set(ChooseFileAction.CustomTypeString, new ChooseFileParser());
    }

    public async Task RenderAsync()
    {
        await RenderWidgetFrameworkElementAsync();
    }

    private async Task RenderWidgetFrameworkElementAsync()
    {
        await Task.Run(async () =>
        {
            var cardTemplate = await Widget.GetCardTemplateAsync();
            var cardData = await Widget.GetCardDataAsync();

            if (string.IsNullOrEmpty(cardData) || string.IsNullOrEmpty(cardTemplate))
            {
                _log.Warning("Widget.GetCardDataAsync returned empty, cannot render card.");
                ShowErrorCard("WidgetErrorCardDisplayText");
                return;
            }

            // Uncomment for extra debugging output
            // _log.Debug($"cardTemplate = {cardTemplate}");
            // _log.Debug($"cardData = {cardData}");

            // Use the data to fill in the template.
            AdaptiveCardParseResult card;
            try
            {
                var template = new AdaptiveCardTemplate(cardTemplate);

                var hostData = new JsonObject
                {
                    // DevHomeTODO: Add support to host theme in hostData
                    { "widgetSize", WidgetSize.ToString().ToLowerInvariant() }, // "small", "medium" or "large"
                }.ToString();

                var context = new EvaluationContext(cardData, hostData);
                var json = template.Expand(context);

                // Create adaptive card.
                card = AdaptiveCard.FromJsonString(json, _elementParser, _actionParser);
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "There was an error expanding the Widget template with data: ");
                ShowErrorCard("WidgetErrorCardDisplayText");
                return;
            }

            if (_renderedCard != null)
            {
                _renderedCard.Action -= HandleAdaptiveAction;
            }

            if (card == null || card.AdaptiveCard == null)
            {
                _log.Error("Error in AdaptiveCardParseResult");
                ShowErrorCard("WidgetErrorCardDisplayText");
                return;
            }

            // Render card on the UI thread.
            _dispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    var renderer = await _renderingService.GetRendererAsync();
                    _renderedCard = renderer.RenderAdaptiveCard(card.AdaptiveCard);
                    if (_renderedCard != null && _renderedCard.FrameworkElement != null)
                    {
                        _renderedCard.Action += HandleAdaptiveAction;
                        WidgetFrameworkElement = _renderedCard.FrameworkElement;
                        AnnounceWarnings(card.AdaptiveCard);
                    }
                    else
                    {
                        _log.Error("Error in RenderedAdaptiveCard");
                        WidgetFrameworkElement = GetErrorCard("WidgetErrorCardDisplayText");
                    }

                    // Set IsLoaded to true after the card is rendered.
                    IsLoaded = true;
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error rendering widget card: ");
                    WidgetFrameworkElement = GetErrorCard("WidgetErrorCardDisplayText");
                }
            });
        });
    }

    private async Task<bool> IsWidgetContentAvailable()
    {
        return await Task.Run(async () =>
        {
            var cardTemplate = await Widget.GetCardTemplateAsync();
            var cardData = await Widget.GetCardDataAsync();

            if (string.IsNullOrEmpty(cardTemplate) || string.IsNullOrEmpty(cardData))
            {
                _log.Debug("Widget content not available yet.");
                return false;
            }

            _log.Debug("Widget content available.");
            return true;
        });
    }

    // If widget content (fresh or cached) is available, show it.
    // Otherwise, show the loading card until the widget updates itself.
    private async void ShowWidgetContentIfAvailable()
    {
        if (await IsWidgetContentAvailable())
        {
            await RenderWidgetFrameworkElementAsync();
        }
        else
        {
            ShowLoadingCard();
        }
    }

    // Used to show a loading ring when we don't have widget content.
    public void ShowLoadingCard()
    {
        _log.Debug("Show loading card.");
        _dispatcherQueue.TryEnqueue(() =>
        {
            WidgetFrameworkElement = new ProgressRing();
        });
    }

    // Used to show a message instead of Adaptive Card content in a widget.
    public void ShowErrorCard(string error, string subError = null!)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            WidgetFrameworkElement = GetErrorCard(error, subError);
        });
    }

    private static Grid GetErrorCard(string error, string subError = null!)
    {
        var grid = new Grid
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
            Padding = new Thickness(15, 0, 15, 0),
        };

        var sp = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        var errorText = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.WrapWholeWords,
            FontWeight = FontWeights.Bold,
            Text = error.GetLocalizedString(Constants.DevHomeDashboard),
        };
        sp.Children.Add(errorText);

        if (subError is not null)
        {
            var subErrorText = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = subError.GetLocalizedString(Constants.DevHomeDashboard),
                Margin = new Thickness(0, 12, 0, 0),
            };

            sp.Children.Add(subErrorText);
        }

        grid.Children.Add(sp);
        return grid;
    }

    private static JObject WrapJsonString(string jsonString)
    {
        return new JObject { ["data"] = jsonString };
    }

    private static string MergeJsonData(Windows.Data.Json.JsonValue actionValue, Windows.Data.Json.JsonObject inputsObject)
    {
        JObject objA = [];
        JObject objB = [];

        if (actionValue?.ValueType == Windows.Data.Json.JsonValueType.Object)
        {
            objA = JObject.Parse(actionValue.Stringify());
        }
        else if (actionValue?.ValueType == Windows.Data.Json.JsonValueType.String)
        {
            objA = WrapJsonString(actionValue.Stringify());
        }

        if (inputsObject?.ValueType != Windows.Data.Json.JsonValueType.Null)
        {
            objB = JObject.Parse(inputsObject!.Stringify());
        }

        objA.Merge(objB);

        return objA.ToString();
    }

    private async void HandleAdaptiveAction(RenderedAdaptiveCard sender, AdaptiveActionEventArgs args)
    {
        _log.Information($"HandleInvokedAction {args.Action.ActionTypeString} for widget {Widget.Id}");
        if (args.Action is AdaptiveOpenUrlAction openUrlAction)
        {
            _log.Information($"Url = {openUrlAction.Url}");
            await Windows.System.Launcher.LaunchUriAsync(openUrlAction.Url);
        }
        else if (args.Action is AdaptiveExecuteAction executeAction)
        {
            var actionValue = executeAction.DataJson;
            var inputsObject = args.Inputs.AsJson();

            var dataToSend = MergeJsonData(actionValue, inputsObject);

            _log.Information($"Verb = {executeAction.Verb}, Data = {dataToSend}");
            await Widget.NotifyActionInvokedAsync(executeAction.Verb, dataToSend);
        }
        else if (args.Action is ChooseFileAction filePickerAction)
        {
            var dataToSend = string.Empty;
            if (!filePickerAction.LaunchFilePicker())
            {
                // Don't send data if the user canceled the file picker.
                return;
            }

            var dataType = filePickerAction.ToJson().ValueType;
            if (dataType != Windows.Data.Json.JsonValueType.Null)
            {
                dataToSend = filePickerAction.ToJson().Stringify();
            }
            else
            {
                var inputType = args.Inputs.AsJson().ValueType;
                if (inputType != Windows.Data.Json.JsonValueType.Null)
                {
                    dataToSend = args.Inputs.AsJson().Stringify();
                }
            }

            _log.Information($"Verb = {filePickerAction.Verb}, Data = {dataToSend}");
            await Widget.NotifyActionInvokedAsync(filePickerAction.Verb, dataToSend);
        }

        // DevHomeTODO: Handle other ActionTypes
        // https://github.com/microsoft/devhome/issues/644
    }

    private async void HandleWidgetUpdated(ComSafeWidget sender, WidgetUpdatedEventArgs args)
    {
        _log.Debug($"HandleWidgetUpdated for widget {sender.Id}");
        await RenderWidgetFrameworkElementAsync();
    }

    public void UnsubscribeFromWidgetUpdates()
    {
        if (Widget != null)
        {
            Widget.WidgetUpdated -= HandleWidgetUpdated;
        }
    }

    private static void AnnounceWarnings(AdaptiveCard card)
    {
        if (!AutomationPeer.ListenerExists(AutomationEvents.AutomationFocusChanged))
        {
            return;
        }

        foreach (var element in card.Body)
        {
            SearchForWarning(element, false);
        }
    }

    // We are only interested in plain texts. Buttons, Actions, Images
    // and textboxes are all ignored. Including ActionSets and ImageSets.
    // We are treating any text inside a container with the "Warning" style
    // as an actual warning to be announced.
    // For now, the only types of containers widgets use are Containers and Columns. In the future,
    // we may add Caroussels, Tables and Facts to this list.
    // We just need to add the other controls in this dictionary
    // with the correct function to access its children.
    private static readonly Dictionary<Type, string> _containerTypes = new()
    {
        { typeof(AdaptiveContainer), "get_Items" },
        { typeof(AdaptiveColumn), "get_Items" },
        { typeof(AdaptiveColumnSet), "get_Columns" },
    };

    private static void SearchForWarning(IAdaptiveCardElement element, bool isInsideWarningContainer)
    {
        if (element is AdaptiveTextBlock textBlock)
        {
            return;
        }

        if (element is not IAdaptiveContainerBase)
        {
            return;
        }

        var containerElement = element as IAdaptiveContainerBase;

        foreach (var containerType in _containerTypes.Where(containerType => containerType.Key == containerElement?.GetType()))
        {
            var itemsMethod = containerType.Key.GetMethod(containerType.Value, BindingFlags.Public | BindingFlags.Instance);

            if (itemsMethod?.Invoke(containerElement, null) is IEnumerable subelements)
            {
                foreach (var subelement in subelements)
                {
                    SearchForWarning((IAdaptiveCardElement)subelement, isInsideWarningContainer || (containerElement?.Style == ContainerStyle.Warning));
                }
            }
        }
    }

    #region Dispose

    private bool _disposed;

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _renderedCard = null!;
                UnsubscribeFromWidgetUpdates();
                WidgetDefinition?.Dispose();
            }

            _disposed = true;
        }
    }

    #endregion
}
