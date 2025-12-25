// BSO (Balatro Seed Oracle) Browser Helper Functions
// These functions wrap localStorage operations for .NET WASM interop

window.BSO = window.BSO || {};

// Test if localStorage is available
window.BSO.testLocalStorage = function() {
    try {
        localStorage.setItem('test', 'test');
        var result = localStorage.getItem('test');
        localStorage.removeItem('test');
        return result === 'test' ? 'LocalStorage works' : 'LocalStorage failed';
    } catch (e) {
        return 'LocalStorage error: ' + e.message;
    }
};

// Wrapper functions for localStorage
window.BSO.getLocalStorageItem = function(key) {
    try {
        return localStorage.getItem(key);
    } catch (e) {
        console.error('Error getting localStorage item:', e);
        return null;
    }
};

window.BSO.setLocalStorageItem = function(key, value) {
    try {
        localStorage.setItem(key, value);
    } catch (e) {
        console.error('Error setting localStorage item:', e);
        throw e;
    }
};

window.BSO.removeLocalStorageItem = function(key) {
    try {
        localStorage.removeItem(key);
    } catch (e) {
        console.error('Error removing localStorage item:', e);
        throw e;
    }
};

window.BSO.getLocalStorageLength = function() {
    try {
        return localStorage.length;
    } catch (e) {
        console.error('Error getting localStorage length:', e);
        return 0;
    }
};

window.BSO.getLocalStorageKey = function(index) {
    try {
        return localStorage.key(index);
    } catch (e) {
        console.error('Error getting localStorage key:', e);
        return null;
    }
};

window.BSO.getLocalStorageKeys = function() {
    try {
        var keys = [];
        for (var i = 0; i < localStorage.length; i++) {
            var key = localStorage.key(i);
            if (key) keys.push(key);
        }
        return keys;
    } catch (e) {
        console.error('Error getting localStorage keys:', e);
        return [];
    }
};

console.log('BSO Browser helper functions loaded');
