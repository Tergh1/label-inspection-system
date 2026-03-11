window.inspectionDefectViewer = (() => {
    let escapeHandler;

    async function getImageDimensions(image) {
        if (!image) {
            return null;
        }

        if (image.complete && image.naturalWidth > 0 && image.naturalHeight > 0) {
            return {
                width: image.naturalWidth,
                height: image.naturalHeight
            };
        }

        return new Promise((resolve) => {
            const finalize = () => {
                cleanup();

                if (image.naturalWidth > 0 && image.naturalHeight > 0) {
                    resolve({
                        width: image.naturalWidth,
                        height: image.naturalHeight
                    });
                    return;
                }

                resolve(null);
            };

            const cleanup = () => {
                image.removeEventListener("load", finalize);
                image.removeEventListener("error", finalize);
            };

            image.addEventListener("load", finalize, { once: true });
            image.addEventListener("error", finalize, { once: true });
        });
    }

    function registerEscapeHandler(dotNetHelper) {
        escapeHandler = async (event) => {
            if (event.key !== "Escape") {
                return;
            }

            await dotNetHelper.invokeMethodAsync("HandleDefectModalEscape");
        };

        document.addEventListener("keydown", escapeHandler);
    }

    function unregisterEscapeHandler() {
        if (!escapeHandler) {
            return;
        }

        document.removeEventListener("keydown", escapeHandler);
        escapeHandler = undefined;
    }

    return {
        getImageDimensions,
        registerEscapeHandler,
        unregisterEscapeHandler
    };
})();
