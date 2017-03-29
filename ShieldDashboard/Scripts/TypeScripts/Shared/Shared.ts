/// <reference path="SharedModules.ts" />

var globalObjects: any = window;
var Urls: any = globalObjects.Url || {};

class Ajax {
    public static GetJSON(url: string, data: any, successFunc: any, errorFunc: any, completeFunc: any): JQueryXHR {
        return Ajax.Get(url, data, successFunc, errorFunc, completeFunc, "json");
    }

    public static PostJSON(url: string, data: any, successFunc: any, errorFunc: any, completeFunc: any): JQueryXHR {
        return Ajax.Post(url, data, successFunc, errorFunc, completeFunc, "json");
    }

    public static GetHtml(url: string, data: any, successFunc: any, errorFunc: any, completeFunc: any): JQueryXHR {
        return Ajax.Get(url, data, successFunc, errorFunc, completeFunc, "html");
    }

    public static Get(url: string, data: any, successFunc: any, errorFunc: any, completeFunc: any, dataType: string):
        JQueryXHR {
        return $.ajax(
            url,
            {
                type: "GET",
                data: data,
                contentType: "application/x-www-form-urlencoded",
                dataType: dataType,
                success: successFunc,
                error: errorFunc,
                complete: completeFunc
            });
    }

    public static Post(url: string, data: any, successFunc: any, errorFunc: any, completeFunc: any, dataType: string): JQueryXHR {
        return $.ajax(
            url,
            {
                type: "POST",
                data: data,
                contentType: "application/x-www-form-urlencoded",
                dataType: dataType,
                success: successFunc,
                error: errorFunc,
                complete: completeFunc
            });
    }
}
