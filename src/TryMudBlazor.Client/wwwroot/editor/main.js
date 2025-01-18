require.config({ paths: { 'vs': 'lib/monaco-editor/min/vs' } });

let _dotNetInstance;

const throttleLastTimeFuncNameMappings = {};

function registerLangugageProvider(language) {
    monaco.languages.registerCompletionItemProvider(language, {
        provideCompletionItems: async function (model, position) {
            var textUntilPosition = model.getValueInRange({
                startLineNumber: 1,
                startColumn: 1,
                endLineNumber: position.lineNumber,
                endColumn: position.column,
            });

            if(language == 'razor')
            {
                if ((textUntilPosition.match(/{/g) || []).length !== (textUntilPosition.match(/}/g) || []).length) {
                    var data = await fetch("editor/snippets/csharp.json").then((response) => response.json());
                } else {
                    var data = await fetch("editor/snippets/mudblazor.json").then((response) => response.json());
                }
            }else {
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
                    detail : data[key].description,
                    documentation : data[key].body.join('\n'),
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
        if (!url) {return; }
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
            iFrame.src = '';
            setTimeout(() => iFrame.src = newSrc);
        }
    },
    dispose: function () {
        _dotNetInstance = null;
        window.removeEventListener('keydown', onKeyDown);
    }
}

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
                    diagnostics:  true,
                    documentFormattingEdits: true,
                    documentHighlights: true,
                    documentRangeFormattingEdits: true,
                });

                registerLangugageProvider('razor');
                registerLangugageProvider('csharp');
            })
        },
        getValue: function () {
            return _editor.getValue();
        },
        setValue: function (value) {
            if(_editor) {
                _editor.setValue(value);
            } else {
                _overrideValue = value;
            }
        },
        focus: function () {
            return _editor.focus();
        },
        setLanguage: function (language) {
            if(_editor) {
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

    function insertAssetsIntoIframe() {
        // Select the iframe
        const iframe = document.getElementById('user-page-window');
        if (!iframe) {
            return;
        }

        const iframeDoc = iframe.contentDocument || iframe.contentWindow.document;

        const insertScript = (src) => {
            const script = iframeDoc.createElement('script');
            script.className = 'try-user-script';
            script.src = src;
            iframeDoc.head.appendChild(script);
        };

        const insertStyle = (href) => {
            const link = iframeDoc.createElement('link');
            link.rel = 'stylesheet';
            link.href = href;
            link.className = 'try-user-style';
            iframeDoc.head.appendChild(link);
        };

        // Extract script elements
        const scriptDiv = document.querySelector('div.scriptAdds');
        if (scriptDiv) {
            Array.from(scriptDiv.children).forEach(span => {
                const src = span.title;
                if (src) {
                    insertScript(src);
                }
            });
        }

        // Extract style elements
        const styleDiv = document.querySelector('div.styleAdds');
        if (styleDiv) {
            Array.from(styleDiv.children).forEach(span => {
                const href = span.title;
                if (href) {
                    insertStyle(href);
                }
            });
        }
    }

    function insertCssContentIntoIframe(cssContent) {
        const iframe = document.getElementById('user-page-window');
        if (!iframe) {
            console.error('Iframe with id "user-page-window" not found.');
            return;
        }

        const iframeDoc = iframe.contentDocument || iframe.contentWindow.document;
        const style = iframeDoc.createElement('style');
        style.className = 'try-user-style';

        const cleanedCssContent = cssContent.replace(/<\/?style[^>]*>/gi, '');
        style.textContent = cleanedCssContent;

        iframeDoc.head.appendChild(style);  // Append style to the end of head
    }

    function insertJsContentIntoIframe(jsContent) {
        const iframe = document.getElementById('user-page-window');
        if (!iframe) {
            console.error('Iframe with id "user-page-window" not found.');
            return;
        }

        const iframeDoc = iframe.contentDocument || iframe.contentWindow.document;
        const script = iframeDoc.createElement('script');
        script.className = 'user-script';
        const cleanedJsContent = jsContent.replace(/<\/?script[^>]*>/gi, '');
        script.textContent = cleanedJsContent;

        iframeDoc.body.appendChild(script);  // Append script to body for immediate execution
    }

    function removeUserScriptsAndStyles() {
        const iframe = document.getElementById('user-page-window');
        if (!iframe) {
            return;
        }

        const iframeDoc = iframe.contentDocument || iframe.contentWindow.document;

        Array.from(iframeDoc.querySelectorAll('script.try-user-script')).forEach(script => {
            script.remove();
        });

        Array.from(iframeDoc.querySelectorAll('style.try-user-style')).forEach(style => {
            style.remove();
        });
    }

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
            const cache = await caches.open('dotnet-resources-/');
            const keys = await cache.keys();
            const dllsData = [];
            await Promise.all(dllNames.map(async (dll) => {
                // Requires WasmFingerprintAssets to be enabled
                const pattern = new RegExp(`${dll}.[^\\.]*\\.dll`, 'i');
                const dllKey = keys.find(x => pattern.test(x.url)).url.substring(window.location.origin.length);
                const response = await cache.match(dllKey);
                const bytes = new Uint8Array(await response.arrayBuffer());
                dllsData.push(bytes);
            }));

            return dllsData;
        },

        updateUserComponentsDll: async function (fileContent) {
            if (!fileContent) {
                return;
            }

            const cache = await caches.open('dotnet-resources-/');

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
