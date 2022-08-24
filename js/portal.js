let currentImageData;

window.wiiuMainApplication = {
    getScreenShot: function (screen) {
        console.log(`Getting screenshot: ${screen}`);

        if (!currentImageData) {
            console.warn(
                "Current image data is undefined, so the image will be broken!"
            );
        }

        return currentImageData;
    },
};

function readFile(input) {
    return new Promise((resolve, reject) => {
        const file = input.files[0];
        if (!file) {
            reject("No file was selected.");
        }

        const reader = new FileReader();
        reader.readAsDataURL(file);

        reader.onload = () => {
            resolve(reader.result);
        };
        reader.onerror = (error) => {
            const errorMessage = `Error loading file ${file}: ${error}`;
            reject(errorMessage);
        };
    });
}
if (document.readyState !== "loading") {
    initInputs();
} else {
    document.addEventListener("DOMContentLoaded", function () {
        initInputs();
    });
}

function initInputs() {
    const hiddenImageInput = document.createElement("input");
    hiddenImageInput.type = "file";
    hiddenImageInput.accept = "image/png";

    hiddenImageInput.onchange = async () => {
        try {
            let imageData = await readFile(hiddenImageInput);
            currentImageData = imageData.replace("data:image/png;base64,", "");
        } catch (ex) {
            console.error(ex);
        }

        // From juxt.js: sets the source of the screenshot preview images
        document.getElementById("post-top-screen-preview").src =
            "data:image/png;base64," +
            window.wiiuMainApplication.getScreenShot(true);
        document.getElementById("post-bottom-screen-preview").src =
            "data:image/png;base64," +
            window.wiiuMainApplication.getScreenShot(false);
    };

    const loadImageButton = document.createElement("button");
    loadImageButton.innerHTML = "Select a screenshot to post";
    loadImageButton.style = "font-size: 24px;";
    loadImageButton.onclick = () => {
        hiddenImageInput.click();
    };
    document.body.appendChild(loadImageButton);
}
