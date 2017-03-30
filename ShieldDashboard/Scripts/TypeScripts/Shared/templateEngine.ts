
//#region References
/// <reference path="SharedModules.ts" />
//#endregion


interface JQueryStatic {
    template:
    {
        (name: string, template: string): string
    };

    tmpl:
    {
        (name: string, input: any): JQuery
    };
}

class TemplateEngine {
    private static Cache: any = {};

    public static Setup(): void {
        $('script[type="text/x-jquery-tmpl"]').each(function (idx: number) {
            var $tmpl: JQuery = $(this);
            var name: string = $tmpl.attr('id').replace('Tmpl', '').replace('Prtl', '');
            TemplateEngine.Cache[name] = {
                'source': $tmpl.html()
            };
            TemplateEngine.Cache[name].template = $.template(name, $tmpl.html());
        });
    }

    public static Render(templateName: string, renderObject: any): string {
        const toStringThing = tokens => tokens.map((index, element) => element['outerHTML']).toArray().join('');

        if (TemplateEngine.Cache[templateName] && TemplateEngine.Cache[templateName].template) {
            return toStringThing(TemplateEngine._Template(templateName, renderObject));
        }
        else if (!TemplateEngine.Cache[templateName]) {
            const $tmpl: JQuery = $('#Tmpl' + templateName);
            if ($tmpl.length === 0) {
                return null;
            }
            TemplateEngine.Cache[templateName] = {
                'source': $tmpl.html()
            }
        }

        TemplateEngine.Cache[templateName].template = $.template(templateName, TemplateEngine.Cache[templateName].source);
        const tokens = TemplateEngine._Template(templateName, renderObject);

        return toStringThing(tokens);
    }

    private static _Template(templateName: string, renderObject: any): JQuery {
        return $.tmpl(templateName, renderObject);
    }
}