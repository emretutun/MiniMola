(()=>{var w=(e,t)=>()=>{try{return t||e((t={exports:{}}).exports,t),t.exports}catch(a){throw t=0,a}};var O=w(()=>{var p=document.getElementById("memory-game-page"),C=[{key:"fish",label:"Bal\u0131k",art:`
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <polygon points="25,50 7,32 7,68"
                         fill="#ffb347"/>
                <ellipse cx="57" cy="50" rx="34" ry="23"
                         fill="#28aee4"/>
                <circle cx="76" cy="43" r="4"
                        fill="#063653"/>
                <path d="M38 50 Q55 63 72 50"
                      fill="none"
                      stroke="#087da7"
                      stroke-width="4"/>
            </svg>`},{key:"star",label:"Deniz y\u0131ld\u0131z\u0131",art:`
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <polygon
                    points="50,8 61,36 91,36 67,55 76,87 50,68 24,87 33,55 9,36 39,36"
                    fill="#ff806d"/>
                <circle cx="42" cy="45" r="3"
                        fill="#9d463d"/>
                <circle cx="58" cy="45" r="3"
                        fill="#9d463d"/>
            </svg>`},{key:"shell",label:"Deniz kabu\u011Fu",art:`
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <path
                    d="M16 66 C14 33 31 16 50 16 C69 16 86 33 84 66 Z"
                    fill="#f4b6d2"/>
                <path d="M50 18 V66 M34 23 L40 66 M66 23 L60 66"
                      fill="none"
                      stroke="#c96f9a"
                      stroke-width="5"
                      stroke-linecap="round"/>
                <rect x="14" y="63" width="72" height="14"
                      rx="7"
                      fill="#de8eb3"/>
            </svg>`},{key:"jelly",label:"Denizanas\u0131",art:`
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <path
                    d="M19 50 C19 25 32 12 50 12 C68 12 81 25 81 50 Z"
                    fill="#a77be8"/>
                <path
                    d="M27 51 C25 68 39 70 33 87
                       M43 51 C42 69 52 72 47 89
                       M58 51 C61 67 53 75 59 89
                       M73 51 C77 68 65 73 70 86"
                    fill="none"
                    stroke="#875ac9"
                    stroke-width="6"
                    stroke-linecap="round"/>
                <circle cx="40" cy="36" r="3"
                        fill="#49306b"/>
                <circle cx="60" cy="36" r="3"
                        fill="#49306b"/>
            </svg>`},{key:"coral",label:"Mercan",art:`
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <path
                    d="M50 86 V30
                       M50 53 L30 35
                       M50 65 L72 43
                       M50 46 L67 24
                       M30 35 L25 19
                       M72 43 L82 28"
                    fill="none"
                    stroke="#ff745f"
                    stroke-width="12"
                    stroke-linecap="round"
                    stroke-linejoin="round"/>
                <ellipse cx="50" cy="88" rx="34" ry="8"
                         fill="#d9c28b"/>
            </svg>`},{key:"turtle",label:"Deniz kaplumba\u011Fas\u0131",art:`
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <ellipse cx="48" cy="52" rx="29" ry="24"
                         fill="#39b98a"/>
                <path d="M28 35 Q48 52 68 35
                         M25 54 Q48 65 71 54"
                      fill="none"
                      stroke="#197d62"
                      stroke-width="5"/>
                <circle cx="80" cy="49" r="11"
                        fill="#64d2a5"/>
                <circle cx="84" cy="46" r="2.5"
                        fill="#063653"/>
                <ellipse cx="23" cy="25" rx="12" ry="6"
                         fill="#64d2a5"
                         transform="rotate(35 23 25)"/>
                <ellipse cx="25" cy="78" rx="12" ry="6"
                         fill="#64d2a5"
                         transform="rotate(-35 25 78)"/>
            </svg>`}],n=null,s=null,i=null,m=!1,u=!1,o=0,c=0,d=0,f=null;p&&x().catch(j);async function x(){let e=await fetch(p.dataset.statusUrl,{method:"GET",credentials:"same-origin",headers:{Accept:"application/json"}});if(!e.ok)throw new Error(`Oyun bilgileri al\u0131namad\u0131: ${e.status}`);n=await e.json(),r("memory-pair-count",n.pairCount),r("memory-reward-points",n.rewardPoints),r("memory-point-balance",n.pointBalance.toLocaleString("tr-TR"));let t=document.getElementById("memory-start-button");t?.addEventListener("click",B),t&&(t.disabled=!1,t.textContent=n.rewardClaimed?"E\u011Flence i\xE7in oyna":"Oyunu ba\u015Flat"),r("memory-overlay-title",n.rewardClaimed?"Bug\xFCnk\xFC \xF6d\xFCl\xFCn\xFC ald\u0131n":"Haf\u0131zan\u0131 test et"),r("memory-overlay-message",n.message),l(n.message,n.rewardClaimed?"success":null)}function B(){!n||u||(k(),s=null,i=null,m=!1,u=!0,o=0,c=0,d=0,r("memory-match-count",o),r("memory-move-count",c),r("memory-elapsed-time",d),M(),document.getElementById("memory-game-overlay")?.classList.add("is-hidden"),l("\u0130lk kart\u0131n\u0131 se\xE7."),f=window.setInterval(()=>{d++,r("memory-elapsed-time",d)},1e3))}function M(){let e=document.getElementById("memory-board");if(!e)return;e.replaceChildren();let t=C.slice(0,n.pairCount);R([...t,...t]).forEach((y,v)=>{e.appendChild(E(y,v))})}function E(e,t){let a=document.createElement("button");return a.type="button",a.className="memory-card",a.dataset.cardKey=e.key,a.setAttribute("aria-label",`${t+1}. kapal\u0131 kart`),a.innerHTML=`
        <span class="memory-card-inner">
            <span class="memory-card-face memory-card-front">
            </span>

            <span class="memory-card-face memory-card-back">
                <span class="memory-card-art">
                    ${e.art}
                </span>
            </span>
        </span>`,a.addEventListener("click",()=>L(a,e)),a}function L(e,t){if(!(!u||m||e===s||e.classList.contains("is-matched")||e.classList.contains("is-flipped"))){if(e.classList.add("is-flipped"),e.setAttribute("aria-label",t.label),!s){s=e,l("\u015Eimdi e\u015Fini bul.");return}i=e,c++,r("memory-move-count",c),m=!0,s.dataset.cardKey===i.dataset.cardKey?T():I()}}function T(){let e=s,t=i;window.setTimeout(()=>{if(e.classList.add("is-matched"),t.classList.add("is-matched"),o++,r("memory-match-count",o),b(),o>=n.pairCount){S();return}l("E\u015Fle\u015Fme bulundu! Devam et.","success")},380)}function I(){let e=s,t=i;window.setTimeout(()=>{e.classList.remove("is-flipped"),t.classList.remove("is-flipped"),e.setAttribute("aria-label","Kapal\u0131 kart"),t.setAttribute("aria-label","Kapal\u0131 kart"),b(),l("E\u015Fle\u015Fmedi, yeniden dene.")},850)}function b(){s=null,i=null,m=!1}async function S(){if(!u)return;u=!1,m=!0,k();let e=document.getElementById("memory-game-overlay"),t=document.getElementById("memory-start-button");if(e?.classList.remove("is-hidden"),t&&(t.disabled=!0),r("memory-overlay-title","T\xFCm e\u015Fle\u015Fmeleri buldun!"),n.rewardClaimed){r("memory-overlay-message",`${c} hamlede ve ${d} saniyede tamamlad\u0131n. Bug\xFCnk\xFC \xF6d\xFCl\xFCn\xFC daha \xF6nce alm\u0131\u015Ft\u0131n.`),l("Yar\u0131n yeniden g\xFCnl\xFCk \xF6d\xFCl kazanabilirsin.","success"),g();return}r("memory-overlay-message","\xD6d\xFCl\xFCn hesab\u0131na aktar\u0131l\u0131yor..."),await z(),g()}async function z(){let e=document.querySelector('input[name="__RequestVerificationToken"]');if(!e?.value){h("G\xFCvenlik anahtar\u0131 bulunamad\u0131. Sayfay\u0131 yenile.");return}try{let t=await fetch(p.dataset.completeUrl,{method:"POST",credentials:"same-origin",headers:{Accept:"application/json","Content-Type":"application/json","X-CSRF-TOKEN":e.value},body:JSON.stringify({matchedPairs:o,moveCount:c})}),a=await t.json().catch(()=>null);if(!t.ok)throw new Error(a?.message??"Oyun \xF6d\xFCl\xFC al\u0131namad\u0131.");n.rewardClaimed=a.rewardClaimed,n.pointBalance=a.pointBalance,r("memory-point-balance",a.pointBalance.toLocaleString("tr-TR")),r("memory-overlay-message",a.message),l(a.message,"success")}catch(t){h(t instanceof Error?t.message:"\xD6d\xFCl al\u0131n\u0131rken bir sorun olu\u015Ftu.")}}function h(e){r("memory-overlay-message",e),l(e,"error")}function g(){let e=document.getElementById("memory-start-button");e&&(e.disabled=!1,e.textContent="Tekrar oyna")}function k(){f!==null&&(window.clearInterval(f),f=null)}function R(e){let t=[...e];for(let a=t.length-1;a>0;a--){let y=Math.floor(Math.random()*(a+1));[t[a],t[y]]=[t[y],t[a]]}return t}function l(e,t=null){let a=document.getElementById("memory-game-message");a&&(a.textContent=e,a.classList.remove("is-success","is-error"),t==="success"&&a.classList.add("is-success"),t==="error"&&a.classList.add("is-error"))}function r(e,t){let a=document.getElementById(e);a&&(a.textContent=t)}function j(e){console.error(e);let t=e instanceof Error?e.message:"Oyun y\xFCklenirken bir sorun olu\u015Ftu.";r("memory-overlay-title","Bir sorun olu\u015Ftu"),r("memory-overlay-message",t),l(t,"error")}});O();})();
//# sourceMappingURL=memory-game.bundle.js.map
