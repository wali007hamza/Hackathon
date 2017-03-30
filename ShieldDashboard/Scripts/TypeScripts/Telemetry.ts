/// <reference path="Shared/Shared.ts" />

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
        Quantiles: IQuantiles
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
                    TelemetryManager.RenderQantileDurations(quantileData, lookBackHours);
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

        private static RenderActivityTypes(suggestionsResponse: string[]): void {
            $('#ActivityTypeList').html(this.RenderDataList(suggestionsResponse));
        }

        private static RenderActivitySubTypes(suggestionsResponse: string[]): void {
            $('#ActivitySubTypeList').html(this.RenderDataList(suggestionsResponse));
        }

        private static RenderQantileDurations(quantileDurations: IQuantileData, currentLookBack: number): void {
            var name = quantileDurations.Name;
            var subType = quantileDurations.SubType;
            $('#InputLookingBack').val(Number(1) + Number(currentLookBack));
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

        public static ClearInput(domName: any): void {
            $(domName.data).val('');
        }
    }

    export function Init(): void {
        TelemetryManagerInstance = new TelemetryManager();
    }
}

$(document).ready(() => {
    Telemetry.Init();

    // Defaults
    $('#InputLookingBack').val("2");

    // Load subtype and metadata suggestions when one of the input fields gets focus.
    $('#InputActivityType').focus(Telemetry.Handlers.LoadActivityTypes);
    $('#InputActivitySubType').focus(Telemetry.Handlers.LoadActivitySubTypes);
    $('#LookBackBtn').click(Telemetry.Handlers.LookBack);
    //$("#SearchClearActivityName").click("#InputActivityName", Telemetry.Handlers.ClearInput);
    //$("#SearchClearActivityType").click("#InputActivityType", Telemetry.Handlers.ClearInput);
    //$("#SearchClearActivitySubType").click("#InputActivitySubType", Telemetry.Handlers.ClearInput);

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