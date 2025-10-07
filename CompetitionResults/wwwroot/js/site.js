
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

    const pinchTransforms = [];
    let ancestor = element;
    while (ancestor) {
        if (ancestor.hasAttribute && ancestor.hasAttribute('data-pinch-zoom-content')) {
            pinchTransforms.push({
                element: ancestor,
                transform: ancestor.style.transform,
                transition: ancestor.style.transition
            });
            ancestor.style.transition = 'none';
            ancestor.style.transform = 'none';
        }
        ancestor = ancestor.parentElement;
    }

    const restoreTransforms = () => {
        while (pinchTransforms.length > 0) {
            const entry = pinchTransforms.pop();
            if (!entry || !entry.element) {
                continue;
            }

            entry.element.style.transform = entry.transform || '';
            entry.element.style.transition = entry.transition || '';
        }
    };

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
    }).finally(restoreTransforms);
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

window.throwersList = window.throwersList || {};

window.throwersList.getSortPreference = function (cookieName) {
    return window.scoresList.getSortPreference(cookieName);
};

window.throwersList.setSortPreference = function (cookieName, value, days) {
    window.scoresList.setSortPreference(cookieName, value, days);
};


(function () {
    const instances = new WeakMap();
    const ZOOM_THRESHOLD = 1.01;

    function ensurePanzoomAvailable() {
        if (typeof Panzoom === 'function') {
            return true;
        }

        console.warn('pinchZoomContainer: Panzoom library is not available.');
        return false;
    }

    function updateZoomClass(element, panzoom) {
        if (!panzoom) {
            return;
        }

        const scale = typeof panzoom.getScale === 'function' ? panzoom.getScale() : 1;
        if (scale > ZOOM_THRESHOLD) {
            element.classList.add('pinch-zoom-container--zoomed');
        } else {
            element.classList.remove('pinch-zoom-container--zoomed');
        }
    }

    function cleanup(element) {
        const entry = instances.get(element);
        if (!entry) {
            return;
        }

        if (entry.wheelListener) {
            element.removeEventListener('wheel', entry.wheelListener);
        }

        if (entry.changeListener && entry.content) {
            entry.content.removeEventListener('panzoomchange', entry.changeListener);
        }

        entry.panzoom.destroy();
        instances.delete(element);
        element.classList.remove('pinch-zoom-container--zoomed');
    }

    window.pinchZoomContainer = window.pinchZoomContainer || {};

    window.pinchZoomContainer.init = function (element, options) {
        if (!element || !ensurePanzoomAvailable()) {
            return;
        }

        const content = element.querySelector('[data-pinch-zoom-content]');
        if (!content) {
            console.warn('pinchZoomContainer: Zoomable content element not found.');
            return;
        }

        cleanup(element);

        const settings = Object.assign({
            minScale: 1,
            maxScale: 8,
            initialScale: 1,
            enableWheel: true
        }, options || {});

        const minScale = Math.max(0.1, Number(settings.minScale) || 1);
        const maxScale = Math.max(minScale, Number(settings.maxScale) || minScale);
        const initialScale = Math.min(Math.max(Number(settings.initialScale) || minScale, minScale), maxScale);
        const enableWheel = settings.enableWheel !== false;

        if (!content.style.transformOrigin) {
            content.style.transformOrigin = '0 0';
        }
        content.style.willChange = 'transform';

        const panzoom = Panzoom(content, {
            minScale,
            maxScale,
            startScale: initialScale,
            contain: 'outside',
            panOnlyWhenZoomed: true
        });

        const entry = {
            panzoom,
            content,
            initialScale
        };

        if (enableWheel) {
            const wheelListener = function (event) {
                if (!event.ctrlKey) {
                    return;
                }

                event.preventDefault();
                panzoom.zoomWithWheel(event);
            };
            element.addEventListener('wheel', wheelListener, { passive: false });
            entry.wheelListener = wheelListener;
        }

        const changeListener = function () {
            updateZoomClass(element, panzoom);
        };
        content.addEventListener('panzoomchange', changeListener);
        entry.changeListener = changeListener;

        updateZoomClass(element, panzoom);

        instances.set(element, entry);
    };

    window.pinchZoomContainer.reset = function (element) {
        const entry = instances.get(element);
        if (!entry) {
            return;
        }

        entry.panzoom.zoom(entry.initialScale, { animate: false });
        entry.panzoom.pan(0, 0, { animate: false });
        updateZoomClass(element, entry.panzoom);
    };

    window.pinchZoomContainer.dispose = function (element) {
        if (!element) {
            return;
        }

        cleanup(element);
    };
})();

