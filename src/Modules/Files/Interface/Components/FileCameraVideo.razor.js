/**
 * Handles camera access and recording video.
 */
const _recorders = new Map();

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
        _recorders.delete(stream);
    }
    if (videoElement) {
        videoElement.srcObject = null;
    }
}

export function startRecording(stream) {
    const recordedChunks = [];
    const mediaRecorder = new MediaRecorder(stream);

    mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
            recordedChunks.push(event.data);
        }
    };

    _recorders.set(stream, { mediaRecorder, recordedChunks });
    mediaRecorder.start();
}

export async function stopRecording(stream) {
    const data = _recorders.get(stream);
    if (!data) return null;

    const { mediaRecorder, recordedChunks } = data;

    return new Promise((resolve) => {
        mediaRecorder.onstop = () => {
            const blob = new Blob(recordedChunks, { type: "video/webm" });
            const file = new File([blob], `video_${new Date().getTime()}.webm`, { type: "video/webm" });
            _recorders.delete(stream);
            resolve(file);
        };
        mediaRecorder.stop();
    });
}
