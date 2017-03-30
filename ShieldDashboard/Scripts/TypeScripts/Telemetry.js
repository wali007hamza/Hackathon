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
        TelemetryManager.prototype.LookBack = function () {
            var activityName = TelemetryManager.ReadQueryInputs().ActivityName;
            var activityType = TelemetryManager.ReadQueryInputs().ActivityType;
            var activitySubType = TelemetryManager.ReadQueryInputs().ActivitySubType;
            var spotTime = TelemetryManager.ReadQueryInputs().SpotTime;
            var lookBackHours = TelemetryManager.ReadQueryInputs().LookBackHours;
            Ajax.GetJSON(Urls.GetQuantileDurations, {
                'spotTime': spotTime,
                'lookBackHours': lookBackHours,
                'activityName': activityName,
                'activityType': activityType,
                'activitySubType': activitySubType
            }, function (response) {
                var quantileData = JSON.parse(response);
                TelemetryManager.RenderQantileDurations(quantileData, lookBackHours);
            }, function () { }, function () { });
        };
        TelemetryManager.ReadQueryInputs = function () {
            var activityName = $('#InputActivityName').val().trim();
            var activityType = $('#InputActivityType').val().trim();
            var activtiySubType = $('#InputActivitySubType').val().trim();
            var spotTime = $("#InputSpotTime").val().trim();
            var lookBackHours = $('#InputLookingBack').val().trim();
            var result = {
                ActivityName: activityName,
                ActivityType: activityType,
                ActivitySubType: activtiySubType,
                SpotTime: spotTime,
                LookBackHours: lookBackHours
            };
            return result;
        };
        TelemetryManager.RenderActivityTypes = function (suggestionsResponse) {
            $('#ActivityTypeList').html(this.RenderDataList(suggestionsResponse));
        };
        TelemetryManager.RenderActivitySubTypes = function (suggestionsResponse) {
            $('#ActivitySubTypeList').html(this.RenderDataList(suggestionsResponse));
        };
        TelemetryManager.RenderQantileDurations = function (quantileDurations, currentLookBack) {
            var name = quantileDurations.Name;
            var subType = quantileDurations.SubType;
            $('#InputLookingBack').val(Number(1) + Number(currentLookBack));
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
        Handlers.LookBack = function () {
            TelemetryManagerInstance.LookBack();
        };
        Handlers.ClearInput = function (domName) {
            $(domName.data).val('');
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
//# sourceMappingURL=Telemetry.js.map