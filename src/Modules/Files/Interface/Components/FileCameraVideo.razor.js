/**
 * Handles camera access and recording video.
 */
let mediaRecorder;
let recordedChunks = [];

export async function startCamera(videoElement) {
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        throw new Error("Camera API is not supported in this browser.");
    }

    try {
        const stream = await navigator.mediaDevices.getUserMedia({
            video: { facingMode: "user" },
            audio: true
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

export function startRecording(stream) {
    recordedChunks = [];
    mediaRecorder = new MediaRecorder(stream);

    mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
            recordedChunks.push(event.data);
        }
    };

    mediaRecorder.start();
}

export async function stopRecording() {
    return new Promise((resolve) => {
        mediaRecorder.onstop = () => {
            const blob = new Blob(recordedChunks, { type: "video/webm" });
            const file = new File([blob], `video_${new Date().getTime()}.webm`, { type: "video/webm" });
            resolve(file);
        };
        mediaRecorder.stop();
    });
}
