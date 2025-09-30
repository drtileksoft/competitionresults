
function triggerFileInput() {
    document.getElementById('fileInput').click();
}

function triggerCompFileInput() {
    document.getElementById('compfileInput').click();
}

function downloadJson(jsonData, filename) {
    // Create a Blob from the JSON data
    const blob = new Blob([jsonData], { type: 'application/json' });
    const url = URL.createObjectURL(blob);

    // Create an anchor (<a>) element and trigger download
    const a = document.createElement('a');
    a.href = url;
    a.download = filename || 'backup.json'; // Default filename if none provided
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);

    // Clean up by revoking the object URL
    URL.revokeObjectURL(url);
}

function generatePDF(htmlId) {
    const element = document.getElementById(htmlId);
    if (!element) {
        console.error("Element not found:", htmlId);
        return;
    }

    const canvasOptions = {
        scale: 3,
        useCORS: true,
        logging: true
    };

    html2canvas(element, canvasOptions).then(canvas => {
        const imgData = canvas.toDataURL("image/jpeg", 1.0);

        const { jsPDF } = window.jspdf;
        const orientation = canvas.width > canvas.height ? "landscape" : "portrait";
        const pdf = new jsPDF({
            orientation: orientation,
            unit: "pt",
            format: "a4"
        });

        const pageWidth = pdf.internal.pageSize.getWidth();
        const pageHeight = pdf.internal.pageSize.getHeight();

        const imgWidth = pageWidth;
        const imgHeight = canvas.height * (imgWidth / canvas.width);

        let remainingHeight = imgHeight;
        let position = 0;

        while (remainingHeight > 0) {
            pdf.addImage(imgData, "JPEG", 0, -position, imgWidth, imgHeight);
            remainingHeight -= pageHeight;
            position += pageHeight;

            if (remainingHeight > 0) {
                pdf.addPage();
            }
        }

        pdf.save("download.pdf");
    }).catch(error => {
        console.error("html2canvas failed:", error);
    });
}

window.scoresList = window.scoresList || {};

window.scoresList.getSortPreference = function (cookieName) {
    if (!cookieName) {
        return null;
    }

    const match = document.cookie.match(new RegExp("(?:^|; )" + cookieName.replace(/([.$?*|{}()\[\]\\/+^])/g, "\\$1") + "=([^;]*)"));
    return match ? decodeURIComponent(match[1]) : null;
};

window.scoresList.setSortPreference = function (cookieName, value, days) {
    if (!cookieName) {
        return;
    }

    let expires = "";
    if (typeof days === "number") {
        const date = new Date();
        date.setTime(date.getTime() + days * 24 * 60 * 60 * 1000);
        expires = "; expires=" + date.toUTCString();
    }

    document.cookie = cookieName + "=" + encodeURIComponent(value ?? "") + expires + "; path=/";
};


