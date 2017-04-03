/// <reference path="SharedModules.ts" />
var globalObjects = window;
var Urls = globalObjects.Url || {};
var Ajax = (function () {
    function Ajax() {
    }
    Ajax.GetJSON = function (url, data, successFunc, errorFunc, completeFunc) {
        return Ajax.Get(url, data, successFunc, errorFunc, completeFunc, "json");
    };
    Ajax.PostJSON = function (url, data, successFunc, errorFunc, completeFunc) {
        return Ajax.Post(url, data, successFunc, errorFunc, completeFunc, "json");
    };
    Ajax.GetHtml = function (url, data, successFunc, errorFunc, completeFunc) {
        return Ajax.Get(url, data, successFunc, errorFunc, completeFunc, "html");
    };
    Ajax.Get = function (url, data, successFunc, errorFunc, completeFunc, dataType) {
        return $.ajax(url, {
            type: "GET",
            data: data,
            contentType: "application/x-www-form-urlencoded",
            dataType: dataType,
            success: successFunc,
            error: errorFunc,
            complete: completeFunc
        });
    };
    Ajax.Post = function (url, data, successFunc, errorFunc, completeFunc, dataType) {
        return $.ajax(url, {
            type: "POST",
            data: data,
            contentType: "application/x-www-form-urlencoded",
            dataType: dataType,
            success: successFunc,
            error: errorFunc,
            complete: completeFunc
        });
    };
    return Ajax;
}());
//# sourceMappingURL=Shared.js.map