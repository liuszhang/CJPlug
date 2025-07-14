export async function initHandsonTable(elementId) {
    try {
        //await import("./hst/handsontable.full.min.js");

        console.log("Loading Handsontable..." + elementId);

        //var hst = import("./hst/handsontable.full.min.js");
        //const HandsontableModule = await import("./hst/handsontable.full.min.js");

        //const Handsontable = HandsontableModule.default || HandsontableModule;

        //const { default: Handsontable } = await import("./hst/handsontable.full.min.js");

        //await import("./hst/handsontable.min.css");
        //await import("./hst/ht-theme-main.min.css");

        const element = document.getElementById(elementId);

        new Handsontable(element, {
            data: [
                { company: "Tagcat", country: "United Kingdom", rating: 4.4 },
                { company: "Zoomzone", country: "Japan", rating: 4.5 },
                { company: "Meeveo", country: "United States", rating: 4.6 },
            ],
            columns: [
                { data: "company", title: "Company", width: 100 },
                { data: "country", title: "Country", width: 170, type: "dropdown", source: ["United Kingdom", "Japan", "United States"] },
                { data: "rating", title: "Rating", width: 100, type: "numeric" },
            ],
            rowHeaders: true,
            navigableHeaders: true,
            tabNavigation: true,
            multiColumnSorting: true,
            headerClassName: "htLeft",
            licenseKey: "non-commercial-and-evaluation",
        });
    }
    catch (error) {
        console.error("Error loading Handsontable:", error);
    }
}
