let recorder;
let dotNetRef;

export async function supports() {
    return navigator.mediaDevices && MediaRecorder.isTypeSupported('audio/webm');
}

export async function start(dotNet) {
    dotNetRef = dotNet;
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    recorder = new MediaRecorder(stream);

    recorder.ondataavailable = async e => {
        if (!dotNetRef) {
            return;
        }
        if (e.data && e.data.size > 0) {
            const buffer = new Uint8Array(await e.data.arrayBuffer());
            await dotNetRef.invokeMethodAsync('OnChunk', buffer);
        }
    };

    recorder.onstop = () => {
        if (!dotNetRef) {
            return;
        }
        dotNetRef.invokeMethodAsync('OnStop');
        stream.getTracks().forEach(t => t.stop());
    };

    recorder.start(250);
}

export function stop() {
    if (recorder) {
        recorder.ondataavailable = null;
        recorder.stream.getTracks().forEach(track => track.stop());
        recorder.stop();
        recorder = null;
        dotNetRef = null;
    }
}
