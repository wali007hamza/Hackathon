/// <reference path="Shared/Shared.ts" />
/// <reference path="../Definitions/chart.d.ts" />

module Telemetry {
    interface IActivityListResponse {
        ActivityName: string[]
    }

    interface ITypeListResponse {
        TypeName: string[]
    }

    interface ISubTypeResponse {
        SubTypeName: string[]
    }

    interface IQueryParameters {
        ActivityName: string;
        ActivityType: string;
        ActivitySubType: string;
        SpotTime: string;
        LookBackHours: number;
    }

    interface IQuantileData {
        Name: string;
        Type: string;
        SubType: string;
        QuantileDurations: IQuantileDuration[];
    }

    interface IQuantileDuration {
        DateTime: string;
        Count: number;
        Quantiles: IQuantiles;
    }

    interface IQuantiles {
        Item1: number;
        Item2: number;
        Item3: number;
        Item4: number;
        Item5: number;
        Item6: number;
    }

    class TelemetryManager {
        public LoadActivityTypes(): void {
            var activityName: string = TelemetryManager.ReadQueryInputs().ActivityName;

            Ajax.GetJSON(
                Urls.GetActivityTypesForName,
                {
                    'activityName': activityName
                },
                (response) => {
                    TelemetryManager.RenderActivityTypes(response);
                },
                () => { },
                () => { });
        }

        public LoadActivitySubTypes(): void {
            var activityName: string = TelemetryManager.ReadQueryInputs().ActivityName;
            var activityType: string = TelemetryManager.ReadQueryInputs().ActivityType;

            Ajax.GetJSON(
                Urls.GetActivitySubTypesForNameAndType,
                {
                    'activityName': activityName,
                    'activityType': activityType
                },
                (response) => {
                    TelemetryManager.RenderActivitySubTypes(response);
                },
                () => { },
                () => { });
        }

        public LookBack(): void {
            var activityName: string = TelemetryManager.ReadQueryInputs().ActivityName;
            var activityType: string = TelemetryManager.ReadQueryInputs().ActivityType;
            var activitySubType: string = TelemetryManager.ReadQueryInputs().ActivitySubType;
            var spotTime: string = TelemetryManager.ReadQueryInputs().SpotTime;
            var lookBackHours: number = TelemetryManager.ReadQueryInputs().LookBackHours;

            lookBackHours = Number(1) + Number(lookBackHours);
            Ajax.GetJSON(
                Urls.GetQuantileDurations,
                {
                    'spotTime': spotTime,
                    'lookBackHours': lookBackHours,
                    'activityName': activityName,
                    'activityType': activityType,
                    'activitySubType': activitySubType
                },
                (response) => {
                    var quantileData = JSON.parse(response);
                    TelemetryManager.RenderPlots(quantileData, lookBackHours);
                },
                () => { },
                () => { });
        }

        public Load(): void {
            var activityName: string = TelemetryManager.ReadQueryInputs().ActivityName;
            var activityType: string = TelemetryManager.ReadQueryInputs().ActivityType;
            var activitySubType: string = TelemetryManager.ReadQueryInputs().ActivitySubType;
            var spotTime: string = TelemetryManager.ReadQueryInputs().SpotTime;
            var lookBackHours: number = TelemetryManager.ReadQueryInputs().LookBackHours;

            Ajax.GetJSON(
                Urls.GetQuantileDurations,
                {
                    'spotTime': spotTime,
                    'lookBackHours': lookBackHours,
                    'activityName': activityName,
                    'activityType': activityType,
                    'activitySubType': activitySubType
                },
                (response) => {
                    var quantileData = JSON.parse(response);
                    TelemetryManager.RenderPlots(quantileData, lookBackHours);
                },
                () => { },
                () => { });
        }

        public static ReadQueryInputs(): IQueryParameters {
            var activityName: string = $('#InputActivityName').val().trim();
            var activityType: string = $('#InputActivityType').val().trim();
            var activtiySubType: string = $('#InputActivitySubType').val().trim();
            var spotTime: string = $("#InputSpotTime").val().trim();
            var lookBackHours: number = $('#InputLookingBack').val().trim();

            var result: IQueryParameters = {
                ActivityName: activityName,
                ActivityType: activityType,
                ActivitySubType: activtiySubType,
                SpotTime: spotTime,
                LookBackHours: lookBackHours
            }

            return result;
        }

        public static RenderQuantilePlot(quantileData: IQuantileData): void {
            $('#ResultsContainer').removeClass('hidden');
            $('#divider').removeClass('hidden');
            $('#divider1').removeClass('hidden');
            var chartData: LinearChartData = TelemetryManager.CreateDurationChartData(quantileData);
            TelemetryManager.RenderLineChart(chartData, 'QuantileDurationPlaceholdler');
        }

        public static RenderTotalTrafficPlot(quantileData: IQuantileData): void {
            $('#ResultsContainer').removeClass('hidden');
            $('#divider').removeClass('hidden');
            $('#divider1').removeClass('hidden');
            var chartData: LinearChartData = TelemetryManager.CreateTotalTrafficChartData(quantileData);
            TelemetryManager.RenderLineChart(chartData, 'TotalTrafficPlaceholdler');
        }

        private static CreateTotalTrafficChartData(quantileData: IQuantileData): LinearChartData {
            var datasets: ChartDataSet[] = [];
            var dateLabels: string[] = quantileData.QuantileDurations.map(q => q.DateTime);
            var chartData: number[] = [];
            quantileData.QuantileDurations.forEach((q) => {
                chartData.push(q.Count);
            });

            datasets.push(TelemetryManager.CreateChartDataSet(chartData, "", [50, 50, 50]));
            var result: LinearChartData =
                {
                    labels: dateLabels,
                    datasets: datasets
                };

            return result;
        }

        private static CreateDurationChartData(quantileData: IQuantileData): LinearChartData {
            var datasets: ChartDataSet[] = [];

            var dateLabels: string[] = quantileData.QuantileDurations.map(q => q.DateTime);
            var scaling: number = 170 / 6;

            var chartData: { [key: string]: number[]; } = {};
            chartData["0.5"] = [];
            chartData["0.75"] = [];
            chartData["0.99"] = [];
            chartData["0.999"] = [];
            chartData["0.9999"] = [];
            chartData["0.99995"] = [];
            // Create data set for every quantile and append to datasets.
            quantileData.QuantileDurations.forEach((q) => {
                chartData["0.5"].push(q.Quantiles.Item1);
                chartData["0.75"].push(q.Quantiles.Item2);
                chartData["0.99"].push(q.Quantiles.Item3);
                chartData["0.999"].push(q.Quantiles.Item4);
                chartData["0.9999"].push(q.Quantiles.Item5);
                chartData["0.99995"].push(q.Quantiles.Item6);
            });

            var idx = 0;
            for (var key in chartData) {
                datasets.push(TelemetryManager
                    .CreateChartDataSet(chartData[key],
                    "p(" + key + ")",
                    [50 + (idx + 1) * scaling, 220 - (idx + 1) * scaling, 50]));
                idx++;
            }

            var result: LinearChartData =
                {
                    labels: dateLabels,
                    datasets: datasets
                };

            return result;
        }

        private static CreateChartDataSet(values: number[], label: string, color: number[]): ChartDataSet {
            // css colors may only have whole numbers, no decimal parts.
            var colorStr: string[] = color.map(x => x.toFixed(0));

            var result: ChartDataSet =
                {
                    label: label,
                    fillColor: "rgba(0,0,0,0)",
                    strokeColor: "rgba(" + colorStr[0] + "," + colorStr[1] + "," + colorStr[2] + ",1)",
                    pointColor: "rgba(" + colorStr[0] + "," + colorStr[1] + "," + colorStr[2] + ",1)",
                    pointStrokeColor: "#fff",
                    data: values
                };

            return result;
        }

        private static RenderLineChart(chartData: LinearChartData, placeHolder: string): void {
            var canvasId: string = placeHolder + 'Canvas';
            var legendId: string = placeHolder + 'Legend';

            var tmpl: string = "<div class='PlotWrapper'><canvas class='PlotCanvas' id='" +
                canvasId +
                "'></canvas></div><div class='LegendWrapper' id='" +
                legendId +
                "'></div><div class='ClearBoth'></div>";
            $('#' + placeHolder).html(tmpl);
            var ctx: CanvasRenderingContext2D = $('#' + canvasId).get(0).getContext('2d');

            var chart: LinearInstance = new Chart(ctx).Line(chartData);

            $('#' + legendId).html(chart.generateLegend());
        }

        private static RenderActivityTypes(suggestionsResponse: string[]): void {
            $('#ActivityTypeList').html(this.RenderDataList(suggestionsResponse));
        }

        private static RenderActivitySubTypes(suggestionsResponse: string[]): void {
            $('#ActivitySubTypeList').html(this.RenderDataList(suggestionsResponse));
        }

        private static RenderPlots(quantileDurations: IQuantileData, lookbackHours: number): void {
            TelemetryManager.RenderQuantilePlot(quantileDurations);
            TelemetryManager.RenderTotalTrafficPlot(quantileDurations);
            $('#InputLookingBack').val(lookbackHours);
        }

        private static RenderDataList(optionList: string[]): string {
            var generatedOptions = '';
            for (var i = 0; i < optionList.length; i++) {
                generatedOptions += '<option value="' + optionList[i] + '" />';
            }

            return generatedOptions;
        }
    }

    var TelemetryManagerInstance: TelemetryManager;

    export class Handlers {
        public static LoadActivityTypes(): void {
            TelemetryManagerInstance.LoadActivityTypes();
        }

        public static LoadActivitySubTypes(): void {
            TelemetryManagerInstance.LoadActivitySubTypes();
        }

        public static LookBack(): void {
            TelemetryManagerInstance.LookBack();
        }

        public static Load(): void {
            TelemetryManagerInstance.Load();
        }

        public static ClearActivityName(): void {
            $("#InputActivityName").val('');
            $("#InputActivityType").val('');
            $("#InputActivitySubType").val('');
        }

        public static ClearActivityType(): void {
            $("#InputActivityType").val('');
            $("#InputActivitySubType").val('');
        }
    }

    export function Init(): void {
        TelemetryManagerInstance = new TelemetryManager();
        //// Set global plot options.
        //// Show dataset label in tooltips.
        //Chart.defaults.global.multiTooltipTemplate = "<%if (datasetLabel){%><%=datasetLabel%>: <%}%><%=value %>";
        //// Add a leading space to scale labels to work around truncation issues.
        //Chart.defaults.global.scaleLabel = " <%=value%>";
    }
}

$(document).ready(() => {
    Telemetry.Init();

    // Defaults
    $('#InputLookingBack').val("2");

    // Load subtype and metadata suggestions when one of the input fields gets focus.
    $('#InputActivityType').focus(Telemetry.Handlers.LoadActivityTypes);
    $('#InputActivitySubType').focus(Telemetry.Handlers.LoadActivitySubTypes);
    $('#LoadBtn').click(Telemetry.Handlers.Load);
    $('#LookBackBtn').click(Telemetry.Handlers.LookBack);
    $("#SearchClearActivityName").click(Telemetry.Handlers.ClearActivityName);
    $("#SearchClearActivityType").click("#InputActivityType", Telemetry.Handlers.ClearActivityType);

    $('.has-clear input[type="text"]').on('input propertychange', function () {
        var $this = $(this);
        var visible = Boolean($this.val());
        $this.siblings('.form-control-clear').toggleClass('hidden', !visible);
    }).trigger('propertychange');

    $('.form-control-clear').click(function () {
        $(this).siblings('input[type="text"]').val('')
            .trigger('propertychange').focus();
    });
});