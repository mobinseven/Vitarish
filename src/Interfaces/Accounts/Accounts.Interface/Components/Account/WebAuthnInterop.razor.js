/**
 * WebAuthn interop module for Blazor components.
 * Handles passkey registration and authentication via navigator.credentials API.
 */

/**
 * Converts a base64url-encoded string to a Uint8Array.
 * @param {string} base64url - The base64url-encoded string.
 * @returns {Uint8Array} The decoded byte array.
 */
function base64UrlToUint8Array(base64url) {
    const base64 = base64url.replace(/-/g, '+').replace(/_/g, '/');
    const paddedBase64 = base64.padEnd(base64.length + (4 - base64.length % 4) % 4, '=');
    const binary = atob(paddedBase64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i);
    }
    return bytes;
}

/**
 * Converts an ArrayBuffer to a base64url-encoded string.
 * @param {ArrayBuffer} buffer - The array buffer to convert.
 * @returns {string} The base64url-encoded string.
 */
function arrayBufferToBase64Url(buffer) {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.length; i++) {
        binary += String.fromCharCode(bytes[i]);
    }
    const base64 = btoa(binary);
    return base64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

/**
 * Checks if WebAuthn is supported in the current browser.
 * @returns {boolean} True if WebAuthn is supported.
 */
export function isWebAuthnSupported() {
    return !!(
        window.PublicKeyCredential &&
        navigator.credentials &&
        typeof navigator.credentials.create === 'function' &&
        typeof navigator.credentials.get === 'function'
    );
}

/**
 * Checks if the browser supports platform authenticators (e.g., Touch ID, Windows Hello).
 * @returns {Promise<boolean>} True if platform authenticator is available.
 */
export async function isPlatformAuthenticatorAvailable() {
    if (!isWebAuthnSupported()) {
        return false;
    }
    try {
        return await PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable();
    } catch (error) {
        console.error("Error checking for platform authenticator availability:", error);
        return false;
    }
}

/**
 * Begins WebAuthn registration by calling navigator.credentials.create.
 * @param {object} options - The credential creation options from the server.
 * @returns {Promise<object>} The attestation response to send to the server.
 */
export async function createCredential(options) {
    if (!isWebAuthnSupported()) {
        throw new Error('WebAuthn is not supported in this browser');
    }

    try {
        // Convert server options to WebAuthn format
        const publicKeyOptions = {
            challenge: base64UrlToUint8Array(options.challenge),
            rp: {
                id: options.rp.id,
                name: options.rp.name
            },
            user: {
                id: base64UrlToUint8Array(options.user.id),
                name: options.user.name,
                displayName: options.user.displayName
            },
            pubKeyCredParams: options.pubKeyCredParams.map(p => ({
                type: p.type,
                alg: p.alg
            })),
            timeout: options.timeout || 60000,
            attestation: options.attestation || 'none',
            authenticatorSelection: options.authenticatorSelection || {}
        };

        // Add excludeCredentials if present
        if (options.excludeCredentials && options.excludeCredentials.length > 0) {
            publicKeyOptions.excludeCredentials = options.excludeCredentials.map(c => ({
                type: c.type,
                id: base64UrlToUint8Array(c.id),
                transports: c.transports
            }));
        }

        // Call the WebAuthn API
        const credential = await navigator.credentials.create({
            publicKey: publicKeyOptions
        });

        if (!credential) {
            throw new Error('Failed to create credential: No credential returned');
        }

        // Format the response for the server
        const response = credential.response;
        return {
            id: credential.id,
            rawId: arrayBufferToBase64Url(credential.rawId),
            type: credential.type,
            response: {
                attestationObject: arrayBufferToBase64Url(response.attestationObject),
                clientDataJSON: arrayBufferToBase64Url(response.clientDataJSON)
            }
        };
    } catch (error) {
        // Enhance error messages
        if (error.name === 'NotAllowedError') {
            throw new Error('User cancelled the credential creation or the operation timed out');
        } else if (error.name === 'AbortError') {
            throw new Error('Credential creation was aborted');
        } else if (error.name === 'NotSupportedError') {
            throw new Error('WebAuthn is not supported or the authenticator does not support the requested algorithm');
        } else if (error.name === 'InvalidStateError') {
            throw new Error('The authenticator is already registered for this user');
        } else if (error.name === 'SecurityError') {
            throw new Error('The operation is insecure or not allowed in this context');
        }
        // Re-throw the original error or our enhanced version
        throw error;
    }
}

/**
 * Begins WebAuthn authentication by calling navigator.credentials.get.
 * @param {object} options - The assertion options from the server.
 * @returns {Promise<object>} The assertion response to send to the server.
 */
export async function getCredential(options) {
    if (!isWebAuthnSupported()) {
        throw new Error('WebAuthn is not supported in this browser');
    }

    try {
        // Convert server options to WebAuthn format
        const publicKeyOptions = {
            challenge: base64UrlToUint8Array(options.challenge),
            timeout: options.timeout || 60000,
            rpId: options.rpId,
            userVerification: options.userVerification || 'preferred'
        };

        // Add allowCredentials if present
        if (options.allowCredentials && options.allowCredentials.length > 0) {
            publicKeyOptions.allowCredentials = options.allowCredentials.map(c => ({
                type: c.type,
                id: base64UrlToUint8Array(c.id),
                transports: c.transports
            }));
        }

        // Call the WebAuthn API
        const credential = await navigator.credentials.get({
            publicKey: publicKeyOptions
        });

        if (!credential) {
            throw new Error('Failed to get credential: No credential returned');
        }

        // Format the response for the server
        const response = credential.response;
        return {
            id: credential.id,
            rawId: arrayBufferToBase64Url(credential.rawId),
            type: credential.type,
            response: {
                authenticatorData: arrayBufferToBase64Url(response.authenticatorData),
                clientDataJSON: arrayBufferToBase64Url(response.clientDataJSON),
                signature: arrayBufferToBase64Url(response.signature),
                userHandle: response.userHandle ? arrayBufferToBase64Url(response.userHandle) : null
            }
        };
    } catch (error) {
        // Enhance error messages
        if (error.name === 'NotAllowedError') {
            throw new Error('User cancelled the authentication or the operation timed out');
        } else if (error.name === 'AbortError') {
            throw new Error('Authentication was aborted');
        } else if (error.name === 'NotSupportedError') {
            throw new Error('WebAuthn is not supported or the authenticator does not support the requested algorithm');
        } else if (error.name === 'SecurityError') {
            throw new Error('The operation is insecure or not allowed in this context');
        } else if (error.name === 'UnknownError') {
            throw new Error('An unknown error occurred during authentication. Please ensure your authenticator is set up correctly.');
        }
        // Re-throw the original error or our enhanced version
        throw error;
    }
}
