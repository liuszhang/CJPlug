// This is a JavaScript module that is loaded on demand. It can export any number of
// functions, and may import other JavaScript modules if required.

export function showPrompt(message) {
  return prompt(message, 'Type anything here');
}

export async function sheetjs_version(id) {
    /* dynamically import the script in the event listener */
    const XLSX = await import("https://cdn.sheetjs.com/xlsx-0.20.3/package/xlsx.mjs");

    /* use the library */
    return XLSX.version;
}


export function initSpreadsheet(elementId) {
    const XLSX = await import("./js/xlsx.mjs");

    const wb = XLSX.utils.book_new();
    const ws = wb.sheet_new();
    //XLSX.utils.sheet_new();

    //if (data && data.length) {
    //    const ws = XLSX.utils.json_to_sheet(data);
    //    XLSX.utils.book_append_sheet(wb, ws, 'Sheet1');
    //}

    XLSX.utils.sheet_to_html(ws, { id: elementId })

    //const ss = new XLSX.Spreadsheet(elementId, wb);

    //ss.render();

    //return ss;
}

export function initHandsonTable(elementId) {
    try {
        //var hst = await import("./hst/handsontable.full.min.js");

        console.log("Loading Handsontable..." + elementId);

        //var hst = await import("./hst/handsontable.full.min.js");

        //const element = document.getElementById(elementId);

        //new hst.Handsontable(element, {
        //    data: [
        //        { company: "Tagcat", country: "United Kingdom", rating: 4.4 },
        //        { company: "Zoomzone", country: "Japan", rating: 4.5 },
        //        { company: "Meeveo", country: "United States", rating: 4.6 },
        //    ],
        //    columns: [
        //        { data: "company", title: "Company", width: 100 },
        //        { data: "country", title: "Country", width: 170, type: "dropdown", source: ["United Kingdom", "Japan", "United States"] },
        //        { data: "rating", title: "Rating", width: 100, type: "numeric" },
        //    ],
        //    rowHeaders: true,
        //    navigableHeaders: true,
        //    tabNavigation: true,
        //    multiColumnSorting: true,
        //    headerClassName: "htLeft",
        //    licenseKey: "non-commercial-and-evaluation",
        //});
    }
    catch (error) {
        console.error("Error loading Handsontable:", error);
    }


    
}

export function exportToExcel(hot, fileName) {
    const data = hot.getData();
    const ws = XLSX.utils.json_to_sheet(data);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Sheet1');
    XLSX.writeFile(wb, fileName);
}


export async function export_method(...rows) {
    //const XLSX = await import("./js/xlsx.full.min.js");
    //const XLSX = await import("https://cdn.sheetjs.com/xlsx-0.20.3/package/xlsx.mjs");
    const XLSX = await import("./js/xlsx.mjs");
    //const XLSX = (await import('./js/xlsx.full.min.js')).default;

    const ws = XLSX.utils.json_to_sheet(rows);
    const wb = XLSX.utils.book_new(ws, "Data");
    XLSX.writeFile(wb, "SheetJSBlazor.xlsx");
}


export async function export_html(id) {
    const XLSX = await import("https://cdn.sheetjs.com/xlsx-0.20.3/package/xlsx.mjs");
    const wb = XLSX.utils.table_to_book(document.getElementById(id));
    XLSX.writeFile(wb, "SheetJSBlazorHTML.xlsx");
}

