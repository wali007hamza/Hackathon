//#region References
/// <reference path="SharedModules.ts" />
//#endregion
var TemplateEngine = (function () {
    function TemplateEngine() {
    }
    TemplateEngine.Setup = function () {
        $('script[type="text/x-jquery-tmpl"]').each(function (idx) {
            var $tmpl = $(this);
            var name = $tmpl.attr('id').replace('Tmpl', '').replace('Prtl', '');
            TemplateEngine.Cache[name] = {
                'source': $tmpl.html()
            };
            TemplateEngine.Cache[name].template = $.template(name, $tmpl.html());
        });
    };
    TemplateEngine.Render = function (templateName, renderObject) {
        var toStringThing = function (tokens) { return tokens.map(function (index, element) { return element['outerHTML']; }).toArray().join(''); };
        if (TemplateEngine.Cache[templateName] && TemplateEngine.Cache[templateName].template) {
            return toStringThing(TemplateEngine._Template(templateName, renderObject));
        }
        else if (!TemplateEngine.Cache[templateName]) {
            var $tmpl = $('#Tmpl' + templateName);
            if ($tmpl.length === 0) {
                return null;
            }
            TemplateEngine.Cache[templateName] = {
                'source': $tmpl.html()
            };
        }
        TemplateEngine.Cache[templateName].template = $.template(templateName, TemplateEngine.Cache[templateName].source);
        var tokens = TemplateEngine._Template(templateName, renderObject);
        return toStringThing(tokens);
    };
    TemplateEngine._Template = function (templateName, renderObject) {
        return $.tmpl(templateName, renderObject);
    };
    TemplateEngine.Cache = {};
    return TemplateEngine;
}());
//# sourceMappingURL=templateEngine.js.map