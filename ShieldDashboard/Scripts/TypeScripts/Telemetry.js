/// <reference path="Shared/Shared.ts" />
var Telemetry;
(function (Telemetry) {
    var TelemetryManager = (function () {
        function TelemetryManager() {
        }
        TelemetryManager.prototype.LoadActivityTypes = function () {
            var activityName = TelemetryManager.ReadQueryInputs().ActivityName;
            Ajax.GetJSON(Urls.GetActivityTypesForName, {
                'activityName': activityName
            }, function (response) {
                TelemetryManager.RenderActivityTypes(response);
            }, function () { }, function () { });
        };
        TelemetryManager.prototype.LoadActivitySubTypes = function () {
            var activityName = TelemetryManager.ReadQueryInputs().ActivityName;
            var activityType = TelemetryManager.ReadQueryInputs().ActivityType;
            Ajax.GetJSON(Urls.GetActivitySubTypesForNameAndType, {
                'activityName': activityName,
                'activityType': activityType
            }, function (response) {
                TelemetryManager.RenderActivitySubTypes(response);
            }, function () { }, function () { });
        };
        TelemetryManager.ReadQueryInputs = function () {
            var activityName = $('#InputActivityName').val().trim();
            var activityType = $('#InputActivityType').val().trim();
            var activtiySubType = $('#InputActivitySubType').val().trim();
            var rewindHours = $('#InputRewindHours').val().trim();
            var result = {
                ActivityName: activityName,
                ActivityType: activityType,
                ActivitySubType: activtiySubType,
                RewindHours: rewindHours
            };
            return result;
        };
        TelemetryManager.RenderActivityTypes = function (suggestionsResponse) {
            $('#ActivityTypeList').html(this.RenderDataList(suggestionsResponse));
        };
        TelemetryManager.RenderActivitySubTypes = function (suggestionsResponse) {
            $('#ActivitySubTypeList').html(this.RenderDataList(suggestionsResponse));
        };
        TelemetryManager.RenderDataList = function (optionList) {
            var generatedOptions = '';
            for (var i = 0; i < optionList.length; i++) {
                generatedOptions += '<option value="' + optionList[i] + '" />';
            }
            return generatedOptions;
        };
        return TelemetryManager;
    }());
    var TelemetryManagerInstance;
    var Handlers = (function () {
        function Handlers() {
        }
        Handlers.LoadActivityTypes = function () {
            TelemetryManagerInstance.LoadActivityTypes();
        };
        Handlers.LoadActivitySubTypes = function () {
            TelemetryManagerInstance.LoadActivitySubTypes();
        };
        return Handlers;
    }());
    Telemetry.Handlers = Handlers;
    function Init() {
        TelemetryManagerInstance = new TelemetryManager();
    }
    Telemetry.Init = Init;
})(Telemetry || (Telemetry = {}));
$(document).ready(function () {
    Telemetry.Init();
    // Load subtype and metadata suggestions when one of the input fields gets focus.
    $('#InputActivityType').focus(Telemetry.Handlers.LoadActivityTypes);
    $('#InputActivitySubType').focus(Telemetry.Handlers.LoadActivitySubTypes);
});
//# sourceMappingURL=Telemetry.js.map