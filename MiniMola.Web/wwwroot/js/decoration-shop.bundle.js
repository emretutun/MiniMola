(()=>{var v=(e,t)=>()=>{try{return t||e((t={exports:{}}).exports,t),t.exports}catch(n){throw t=0,n}};var b=v(()=>{var d=document.getElementById("decoration-shop");d&&p().catch(C);async function p(){let e=await fetch(d.dataset.apiUrl,{method:"GET",credentials:"same-origin",headers:{Accept:"application/json"}});if(!e.ok)throw new Error(`Ma\u011Faza verisi al\u0131namad\u0131: ${e.status}`);let t=await e.json();k(t),m(t.items)}function k(e){s("decoration-point-balance",e.pointBalance.toLocaleString("tr-TR")),s("decoration-aquarium-level",e.aquariumLevel),s("decoration-placed-count",e.placedDecorationCount)}function m(e){let t=document.getElementById("decoration-shop-grid");if(t){t.replaceChildren();for(let n of e)t.appendChild(w(n))}}function w(e){let t=a("article","decoration-card"),n=a("div","decoration-preview"),o=a("div","decoration-art");o.innerHTML=M(e.assetKey),n.appendChild(o);let r=a("div","decoration-card-content"),c=a("div","decoration-card-topline"),u=a("span","decoration-category",L(e.category)),f=a("span","decoration-owned",`Sende: ${e.ownedCount}`);c.append(u,f);let g=a("h2","",e.name),y=a("p","",e.description),h=a("div","decoration-price");h.innerHTML=`
        <svg viewBox="0 0 20 26"
             width="16"
             height="21"
             aria-hidden="true">
            <path d="M10 1 C10 1 2 12 2 17
                     C2 22 5.5 25 10 25
                     C14.5 25 18 22 18 17
                     C18 12 10 1 10 1 Z"
                  fill="currentColor">
            </path>
        </svg>

        <span>
            ${e.price.toLocaleString("tr-TR")}
            Damla
        </span>`;let i=a("button","buy-decoration-button");return i.type="button",i.disabled=!e.canPurchase,i.textContent=e.canPurchase?"Sat\u0131n al ve yerle\u015Ftir":e.lockedReason??"Kilitli",i.addEventListener("click",()=>x(e,i)),r.append(c,g,y,h,i),t.append(n,r),t}async function x(e,t){let n=document.querySelector('#decoration-antiforgery-form input[name="__RequestVerificationToken"]');if(!n?.value){l("G\xFCvenlik anahtar\u0131 bulunamad\u0131. Sayfay\u0131 yenile.",!1);return}let o=t.textContent;t.disabled=!0,t.classList.add("is-loading"),t.textContent="Akvaryuma ekleniyor...";try{let r=await fetch(`${d.dataset.apiUrl}/${e.id}`,{method:"POST",credentials:"same-origin",headers:{Accept:"application/json","X-CSRF-TOKEN":n.value}}),c=await r.json().catch(()=>null);if(!r.ok)throw new Error(c?.message??`Sat\u0131n alma ba\u015Far\u0131s\u0131z: ${r.status}`);l(c.message,!0),await p()}catch(r){console.error(r),l(r instanceof Error?r.message:"Sat\u0131n alma s\u0131ras\u0131nda bir sorun olu\u015Ftu.",!1),t.disabled=!1,t.textContent=o}finally{t.classList.remove("is-loading")}}function l(e,t){let n=document.getElementById("decoration-shop-message");n&&(n.textContent=e,n.hidden=!1,n.classList.toggle("success",t),n.classList.toggle("error",!t),n.scrollIntoView({behavior:"smooth",block:"nearest"}))}function C(e){console.error(e),document.getElementById("decoration-shop-grid")?.replaceChildren(a("div","shop-loading","Dekorasyon ma\u011Fazas\u0131 y\xFCklenirken bir sorun olu\u015Ftu.")),l("Dekorasyon ma\u011Fazas\u0131na ula\u015F\u0131lamad\u0131.",!1)}function a(e,t,n){let o=document.createElement(e);return t&&(o.className=t),n!==void 0&&(o.textContent=n),o}function s(e,t){let n=document.getElementById(e);n&&(n.textContent=t)}function L(e){return{Plant:"Bitki",Coral:"Mercan",Rock:"Kaya",Ornament:"S\xFCs",Structure:"Yap\u0131",Lighting:"Ayd\u0131nlatma"}[e]??e}function M(e){return{"curved-water-plant":`
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <ellipse cx="50" cy="91" rx="35" ry="7"
                         fill="#b79d67"/>
                <path d="M47 90 C40 72 56 56 43 38
                         C34 26 41 14 48 8"
                      fill="none"
                      stroke="#39b98a"
                      stroke-width="10"
                      stroke-linecap="round"/>
                <path d="M59 90 C66 70 51 57 66 42
                         C75 33 72 21 68 14"
                      fill="none"
                      stroke="#64d2a5"
                      stroke-width="9"
                      stroke-linecap="round"/>
                <path d="M34 90 C28 75 38 65 30 53
                         C25 44 27 34 32 27"
                      fill="none"
                      stroke="#248e70"
                      stroke-width="8"
                      stroke-linecap="round"/>
            </svg>`,"pink-coral":`
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <ellipse cx="50" cy="90" rx="36" ry="7"
                         fill="#b79d67"/>
                <path d="M50 88 V31
                         M50 55 L30 37
                         M50 66 L73 44
                         M50 47 L67 24
                         M30 37 L25 20
                         M73 44 L83 29"
                      fill="none"
                      stroke="#ff7f91"
                      stroke-width="11"
                      stroke-linecap="round"
                      stroke-linejoin="round"/>
            </svg>`,"volcanic-rock":`
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <ellipse cx="50" cy="90" rx="40" ry="7"
                         fill="#b79d67"/>
                <path d="M16 86 L28 43 L45 25
                         L63 34 L84 86 Z"
                      fill="#465b64"/>
                <path d="M28 43 L49 54 L63 34
                         L72 63 L84 86 L16 86 Z"
                      fill="#344951"/>
                <circle cx="43" cy="64" r="7"
                        fill="#243941"/>
                <circle cx="66" cy="73" r="5"
                        fill="#243941"/>
            </svg>`,"treasure-chest":`
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <ellipse cx="50" cy="90" rx="39" ry="7"
                         fill="#b79d67"/>
                <path d="M20 43 C20 27 31 19 50 19
                         C69 19 80 27 80 43 Z"
                      fill="#8c552d"/>
                <rect x="18" y="42"
                      width="64" height="43"
                      rx="5"
                      fill="#9f6232"/>
                <path d="M18 52 H82 M50 19 V85"
                      fill="none"
                      stroke="#e2b34f"
                      stroke-width="7"/>
                <rect x="43" y="54"
                      width="14" height="16"
                      rx="3"
                      fill="#f2ce67"/>
            </svg>`,"mini-lighthouse":`
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <ellipse cx="50" cy="92" rx="36" ry="6"
                         fill="#b79d67"/>
                <path d="M32 88 L39 31 H61 L68 88 Z"
                      fill="#f4eee4"/>
                <path d="M37 50 H63 M35 67 H65"
                      stroke="#e7655d"
                      stroke-width="11"/>
                <rect x="34" y="22"
                      width="32" height="15"
                      rx="3"
                      fill="#f3c85c"/>
                <path d="M29 23 L50 10 L71 23 Z"
                      fill="#d9524b"/>
                <rect x="46" y="73"
                      width="10" height="15"
                      fill="#31566a"/>
            </svg>`,"moon-light":`
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <ellipse cx="50" cy="91" rx="31" ry="6"
                         fill="#b79d67"/>
                <path d="M50 84 V53"
                      stroke="#425f72"
                      stroke-width="7"
                      stroke-linecap="round"/>
                <path d="M30 81 H70"
                      stroke="#425f72"
                      stroke-width="9"
                      stroke-linecap="round"/>
                <path d="M65 15 C47 18 39 34 45 49
                         C50 62 64 67 77 61
                         C67 60 59 52 57 43
                         C54 31 58 22 65 15 Z"
                      fill="#fff1a8"/>
                <circle cx="65" cy="38" r="30"
                        fill="#fff1a8"
                        opacity="0.16"/>
            </svg>`}[e]??`
            <svg viewBox="0 0 100 100" aria-hidden="true">
                <circle cx="50" cy="50" r="30"
                        fill="#65d6d0"/>
            </svg>`}});b();})();
//# sourceMappingURL=decoration-shop.bundle.js.map
