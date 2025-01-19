require.config({ paths: { 'vs': 'lib/monaco-editor/min/vs' } });

let _dotNetInstance;

const throttleLastTimeFuncNameMappings = {};
const CACHE_NAME = 'dotnet-resources-/';

function registerLangugageProvider(language) {
    monaco.languages.registerCompletionItemProvider(language, {
        provideCompletionItems: async function (model, position) {
            var textUntilPosition = model.getValueInRange({
                startLineNumber: 1,
                startColumn: 1,
                endLineNumber: position.lineNumber,
                endColumn: position.column,
            });

            if (language == 'razor') {
                if ((textUntilPosition.match(/{/g) || []).length !== (textUntilPosition.match(/}/g) || []).length) {
                    var data = await fetch("editor/snippets/csharp.json").then((response) => response.json());
                } else {
                    var data = await fetch("editor/snippets/mudblazor.json").then((response) => response.json());
                }
            } else {
                var data = await fetch("editor/snippets/csharp.json").then((response) => response.json());
            }

            var word = model.getWordUntilPosition(position);
            var range = {
                startLineNumber: position.lineNumber,
                endLineNumber: position.lineNumber,
                startColumn: word.startColumn,
                endColumn: word.endColumn,
            };

            var response = Object.keys(data).map(key => {
                return {
                    label: data[key].prefix,
                    detail: data[key].description,
                    documentation: data[key].body.join('\n'),
                    insertText: data[key].body.join('\n'),
                    kind: monaco.languages.CompletionItemKind.Snippet,
                    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                    range: range
                }
            });
            return {
                suggestions: response,
            };
        },
    });
}

function onKeyDown(e) {
    if (e.ctrlKey && e.keyCode === 83) {
        e.preventDefault();

        if (_dotNetInstance && _dotNetInstance.invokeMethodAsync) {
            throttle(() => _dotNetInstance.invokeMethodAsync('TriggerCompileAsync'), 1000, 'compile');
        }
    }
}

function throttle(func, timeFrame, id) {
    const now = new Date();
    if (now - throttleLastTimeFuncNameMappings[id] >= timeFrame) {
        func();

        throttleLastTimeFuncNameMappings[id] = now;
    }
}

window.Try = {
    initialize: function (dotNetInstance) {
        _dotNetInstance = dotNetInstance;
        throttleLastTimeFuncNameMappings['compile'] = new Date();

        Split(['#user-code-editor-container', '#user-page-window-container'], {
            gutterSize: 6,
        })
        window.addEventListener('keydown', onKeyDown);
    },
    changeDisplayUrl: function (url) {
        if (!url) { return; }
        window.history.pushState(null, null, url);
    },
    reloadIframe: function (id, newSrc) {
        const iFrame = document.getElementById(id);
        if (!iFrame) { return; }

        if (!newSrc) {
            iFrame.contentWindow.location.reload();
        } else if (iFrame.src !== `${window.location.origin}${newSrc}`) {
            iFrame.src = newSrc;
        } else {
            // There needs to be some change so the iFrame is actually reloaded
            const randomNum = Math.floor(Math.random() * 10000).toString().padStart(4, '0')
            iFrame.src = `${randomNum}`;
            setTimeout(() => iFrame.src = newSrc);
        }
    },
    clearCache: async function () {
        const cacheName = CACHE_NAME;
        try {
            const cache = await caches.open(cacheName);
            const keys = await cache.keys();

            await Promise.all(keys.map(key => {
                return cache.delete(key);
            }));
            console.log(`Cache '${cacheName}' has been cleared.`);
        } catch (error) {
            console.error('Error clearing cache:', error);
        }
    },
    dispose: function () {
        _dotNetInstance = null;
        window.removeEventListener('keydown', onKeyDown);
    }
}

window.Try.StaticAssets = window.Try.StaticAssets || (function () {
    const CACHE_NAME = 'CACHE_NAME'; // Replace with your actual cache name
    const STATIC_ASSETS_CACHE_NAME = '__static-assets.json';

    let fileContents = {
        scriptLinks: [],
        cssLinks: [],
        inlineScripts: [],
        inlineCss: []
    };

    async function loadFromCache(clear) {
        if ('caches' in window) {
            const cache = await caches.open(CACHE_NAME);
            const response = await cache.match(STATIC_ASSETS_CACHE_NAME);
            if (response && !clear) {
                fileContents = await response.json();
                if (clear) {
                    await cache.delete(STATIC_ASSETS_CACHE_NAME);
                    await cache.put(STATIC_ASSETS_CACHE_NAME, new Response(JSON.stringify(fileContents)));
                }
            } else {
                fileContents = {
                    scriptLinks: [],
                    cssLinks: [],
                    inlineScripts: [],
                    inlineCss: []
                };
                await cache.put(STATIC_ASSETS_CACHE_NAME, new Response(JSON.stringify(fileContents)));
                //console.log('Cache for', STATIC_ASSETS_CACHE_NAME, 'has been initialized.');
            }
        } else {
            console.error('Cache API not supported in this browser');
        }
    }

    function appendStyleToEnd(content) {
        const style = document.createElement('style');
        style.textContent = content;
        style.className = 'try-user-style';
        const allStyles = document.body.querySelectorAll('style');
        if (allStyles.length > 0) {
            allStyles[allStyles.length - 1].after(style);
        } else {
            document.body.appendChild(style);
        }
    }

    return {
        createStaticAssets: async function () {
            await loadFromCache(true);
        },

        saveScriptLink: function (src) {
            if (!src) return;
            fileContents.scriptLinks.push(src);
        },

        insertScriptLinks: function () {
            fileContents.scriptLinks.forEach(src => {
                if (!src) return;
                const script = document.createElement('script');
                script.src = src;
                script.className = 'try-user-script';
                script.type = 'text/javascript';
                document.head.appendChild(script);
            });
        },

        saveCssLink: function (href) {
            if (!href) return;
            fileContents.cssLinks.push(href);
        },

        insertCssLinks: function () {
            fileContents.cssLinks.forEach(href => {
                if (!href) return;
                const link = document.createElement('link');
                link.rel = 'stylesheet';
                link.className = 'try-user-style';
                link.href = href;
                document.head.appendChild(link);
            });
        },

        saveInLineScript: function (content) {
            if (!content) return;
            fileContents.inlineScripts.push(content);
        },

        insertInlineScripts: function () {
            fileContents.inlineScripts.forEach(content => {
                if (!content) return;
                const script = document.createElement('script');
                script.className = 'try-user-script';
                script.textContent = content;
                document.body.appendChild(script);
            });
        },

        saveInlineCss: function (content) {
            if (!content) return;
            fileContents.inlineCss.push(content);
        },

        insertInlineCss: function () {
            fileContents.inlineCss.forEach(content => {
                if (!content) return;
                appendStyleToEnd(content);
            });
        },

        saveStaticAssets: async function () {
            if ('caches' in window) {
                const cache = await caches.open(CACHE_NAME);
                await cache.put(STATIC_ASSETS_CACHE_NAME, new Response(JSON.stringify(fileContents)));
                // console.log('Static assets saved to cache');
            } else {
                console.error('Cache API not supported in this browser');
            }
        },

        loadStaticAssets: async function () {
            await loadFromCache(false);
            //console.log("-- loadStaticAssets --");
            //console.log(this.getFileContents());

            // Insert script links
            this.insertScriptLinks();

            // Insert CSS links
            this.insertCssLinks();

            // Insert inline scripts
            this.insertInlineScripts();

            // Insert inline CSS
            this.insertInlineCss();

            // clear cache so it won't be in my editor area
            await loadFromCache(true);
        },

        getFileContents: function () {
            return fileContents;
        }
    };
})();


window.Try.Editor = window.Try.Editor || (function () {
    let _editor;
    let _overrideValue;

    return {
        create: function (id, value, language) {
            if (!id) { return; }
            let _theme = "default";
            let userPreferences = localStorage.getItem("userPreferences");
            if (userPreferences) {
                const userPreferencesJSON = JSON.parse(userPreferences);
                if (userPreferencesJSON.hasOwnProperty("DarkTheme") && userPreferencesJSON.DarkTheme) {
                    _theme = "vs-dark";
                }
            }

            require(['vs/editor/editor.main'], () => {
                _editor = monaco.editor.create(document.getElementById(id), {
                    value: _overrideValue || value || '',
                    language: language || 'razor',
                    theme: _theme,
                    automaticLayout: true,
                    mouseWheelZoom: true,
                    bracketPairColorization: {
                        enabled: true
                    },
                    minimap: {
                        enabled: false
                    }
                });

                _overrideValue = null;

                monaco.languages.html.razorDefaults.setModeConfiguration({
                    completionItems: true,
                    diagnostics: true,
                    documentFormattingEdits: true,
                    documentHighlights: true,
                    documentRangeFormattingEdits: true,
                });

                registerLangugageProvider('razor');
                registerLangugageProvider('csharp');
                registerLangugageProvider('css');
                registerLangugageProvider('javascript');
            })
        },
        getValue: function () {
            return _editor.getValue();
        },
        setValue: function (value) {
            if (_editor) {
                _editor.setValue(value);
            } else {
                _overrideValue = value;
            }
        },
        focus: function () {
            return _editor.focus();
        },
        setLanguage: function (language) {
            if (_editor) {
                monaco.editor.setModelLanguage(_editor.getModel(), language);
            }
        },
        setTheme: function (theme) {
            monaco.editor.setTheme(theme);
        },
        dispose: function () {
            _editor = null;
        }
    }
}());

window.Try.CodeExecution = window.Try.CodeExecution || (function () {
    const UNEXPECTED_ERROR_MESSAGE = 'An unexpected error has occurred. Please try again later or contact the team.';

    function putInCacheStorage(cache, fileName, fileBytes, contentType) {
        const cachedResponse = new Response(
            new Blob([fileBytes]),
            {
                headers: {
                    'Content-Type': contentType || 'application/octet-stream',
                    'Content-Length': fileBytes.length.toString()
                }
            });

        return cache.put(fileName, cachedResponse);
    }

    function convertBase64StringToBytes(base64String) {
        const binaryString = window.atob(base64String);

        const bytesCount = binaryString.length;
        const bytes = new Uint8Array(bytesCount);
        for (let i = 0; i < bytesCount; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }

        return bytes;
    }

    return {
        getCompilationDlls: async function (dllNames) {
            const cache = await caches.open(CACHE_NAME);
            const keys = await cache.keys();
            const dllsData = [];
            await Promise.all(dllNames.map(async (dll) => {
                // Requires WasmFingerprintAssets to be enabled
                try {
                    const pattern = new RegExp(`${dll}.[^\\.]*\\.dll`, 'i');
                    const dllKey = keys.find(x => pattern.test(x.url)).url.substring(window.location.origin.length);
                    const response = await cache.match(dllKey);
                    const bytes = new Uint8Array(await response.arrayBuffer());
                    dllsData.push(bytes);
                } catch { }
            }));

            return dllsData;
        },

        updateUserComponentsDll: async function (fileContent) {
            if (!fileContent) {
                return;
            }

            const cache = await caches.open(CACHE_NAME);

            const cacheKeys = await cache.keys();
            // Requires WasmFingerprintAssets to be enabled
            const userComponentsDllCacheKey = cacheKeys.find(x => /Try\.UserComponents\.[^/]*\.dll/.test(x.url));
            if (!userComponentsDllCacheKey || !userComponentsDllCacheKey.url) {
                alert(UNEXPECTED_ERROR_MESSAGE);
                return;
            }

            const dllPath = userComponentsDllCacheKey.url.substring(window.location.origin.length);
            fileContent = typeof fileContent === 'number' ? BINDING.conv_string(fileContent) : fileContent // tranfering raw pointer to the memory of the mono string
            const dllBytes = typeof fileContent === 'string' ? convertBase64StringToBytes(fileContent) : fileContent;

            await putInCacheStorage(cache, dllPath, dllBytes);
        }
    };
}());
