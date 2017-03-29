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
        RewindHours: string;
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

        public static ReadQueryInputs(): IQueryParameters {
            var activityName: string = $('#InputActivityName').val().trim();
            var activityType: string = $('#InputActivityType').val().trim();
            var activtiySubType: string = $('#InputActivitySubType').val().trim();
            var rewindHours: string = $('#InputRewindHours').val().trim();

            var result: IQueryParameters = {
                ActivityName: activityName,
                ActivityType: activityType,
                ActivitySubType: activtiySubType,
                RewindHours: rewindHours
            }

            return result;
        }

        private static RenderActivityTypes(suggestionsResponse: string[]): void {
            $('#ActivityTypeList').html(this.RenderDataList(suggestionsResponse));
        }

        private static RenderActivitySubTypes(suggestionsResponse: string[]): void {
            $('#ActivitySubTypeList').html(this.RenderDataList(suggestionsResponse));
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
    }

    export function Init(): void {
        TelemetryManagerInstance = new TelemetryManager();
    }
}

$(document).ready(() => {
    Telemetry.Init();
    // Load subtype and metadata suggestions when one of the input fields gets focus.
    $('#InputActivityType').focus(Telemetry.Handlers.LoadActivityTypes);
    $('#InputActivitySubType').focus(Telemetry.Handlers.LoadActivitySubTypes);
});