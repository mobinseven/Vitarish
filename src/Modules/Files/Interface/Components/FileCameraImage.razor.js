/**
 * Handles camera access and capturing still images.
 */
export async function startCamera(videoElement) {
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        throw new Error("Camera API is not supported in this browser.");
    }

    try {
        const stream = await navigator.mediaDevices.getUserMedia({
            video: { facingMode: "user" },
            audio: false
        });
        videoElement.srcObject = stream;
        videoElement.play();
        return stream;
    } catch (err) {
        console.error("Error accessing camera: ", err);
        throw err;
    }
}

export function stopCamera(videoElement, stream) {
    if (stream) {
        stream.getTracks().forEach(track => track.stop());
    }
    if (videoElement) {
        videoElement.srcObject = null;
    }
}

export async function captureImage(videoElement) {
    const canvas = document.createElement("canvas");
    canvas.width = videoElement.videoWidth;
    canvas.height = videoElement.videoHeight;
    const ctx = canvas.getContext("2d");
    ctx.drawImage(videoElement, 0, 0, canvas.width, canvas.height);

    return new Promise((resolve) => {
        canvas.toBlob((blob) => {
            const file = new File([blob], `capture_${new Date().getTime()}.jpg`, { type: "image/jpeg" });
            resolve(file);
        }, "image/jpeg", 0.9);
    });
}
