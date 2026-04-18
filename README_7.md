# Întrebări conceptuale – Lab07

## 1. De ce Logout este implementat ca `<form method="post">` și nu ca un link `<a href="/Auth/Logout">`?

Logout-ul folosește POST pentru că altfel ar fi vulnerabil la **CSRF (Cross-Site Request Forgery)**. Un link `<a href="/Auth/Logout">` face un request GET, iar orice site extern ar putea să includă acel link și să delogheze utilizatorul fără știrea lui — browserul trimite automat cookie-urile de sesiune.

Prin POST + token CSRF (generat automat de ASP.NET Core), serverul verifică că request-ul vine din formularul nostru, nu din altă parte.

**Ce s-ar întâmpla cu GET?** Oricine ar putea trimite un email cu un link de genul `<img src="https://site.ro/Auth/Logout">` și utilizatorul s-ar delogha automat când deschide emailul.

---

## 2. De ce login-ul face doi pași în loc de unul?

```csharp
var user = await _userManager.FindByEmailAsync(model.Email);       // pasul 1
var result = await _signInManager.PasswordSignInAsync(user.UserName!, ...); // pasul 2
```

ASP.NET Core Identity face distincție clară între **Email** și **UserName** — sunt două câmpuri separate în baza de date. `PasswordSignInAsync` primește **UserName**, nu Email.

Deci primul pas caută userul după email și extrage username-ul, iar al doilea pas face autentificarea efectivă cu acel username.

**De ce nu există `PasswordSignInAsync(email, password)`?** Pentru că Identity nu presupune că email-ul e unic sau că e folosit ca identificator principal — username-ul este cheia de autentificare. Chiar dacă în practică le setăm egale, arhitectura le tratează separat.

---

## 3. De ce nu este suficient să ascunzi butoanele Edit/Delete în View?

```cshtml
@if (User.Identity.IsAuthenticated)
{
    <!-- butoane Edit/Delete -->
}
```

Ascunderea butoanelor în View este doar **UI cosmetic** — nu oferă nicio protecție reală. Oricine poate face un request direct la `/Articles/Edit/5` din browser, Postman sau orice alt tool, fără să treacă prin interfață.

`[Authorize]` + `IsOwnerOrAdmin()` în controller protejează la nivel de **server** — chiar dacă cineva ghicește URL-ul, serverul refuză request-ul.

**Invers: dacă punem doar `[Authorize]` fără să ascundem în View?** Butoanele apar pentru toți utilizatorii autentificați, inclusiv cei care nu sunt proprietarii articolului. Ei vor vedea butoanele, vor da click, dar vor primi o eroare. Este o experiență proastă pentru utilizator — funcționează corect din punct de vedere al securității, dar e confuz și neprofesionist.

---

## 4. Ce este middleware pipeline-ul în ASP.NET Core?

Middleware pipeline-ul este o **serie de componente** care procesează fiecare request HTTP în ordine, unul după altul. Fiecare componentă poate face ceva cu request-ul, apoi îl pasează mai departe, sau poate opri lanțul și returna un răspuns direct.

**De ce `UseAuthentication()` trebuie înaintea `UseAuthorization()`?**

```csharp
app.UseAuthentication(); // întâi: cine ești?
app.UseAuthorization();  // apoi: ce ai voie să faci?
```

Autentificarea trebuie să ruleze prima — ea citește cookie-ul/token-ul și stabilește **cine ești** (populează `User.Identity`). Abia după aceea, autorizarea poate verifica **ce ai voie să faci**.

Dacă le inversăm, în momentul în care `UseAuthorization()` rulează, `User.Identity` nu este încă populat — deci toate verificările de tip `[Authorize]` vor eșua sau vor ignora utilizatorul autentificat, tratându-l ca anonim.

---

## 5. Ce am fi trebuit să implementăm manual fără ASP.NET Core Identity?

Fără Identity, ar fi trebuit să scriem de la zero:

- **Hashing parole** – să nu le stocăm în clar (bcrypt, PBKDF2 etc.)
- **Tabelul de utilizatori** – structura din baza de date, migrările
- **Înregistrare și validare** – unicitate email/username, reguli de complexitate parolă
- **Autentificare** – verificarea parolei hash-uite la login
- **Sesiuni/Cookie-uri** – crearea, semnarea și validarea cookie-ului de sesiune
- **Roluri și permisiuni** – sistem de alocare și verificare roluri
- **Protecție CSRF** – generare și validare token-uri
- **Lockout** – blocare cont după prea multe încercări eșuate
- **Reset parolă** – generare token-uri temporare, trimitere email

Identity face toate acestea out-of-the-box, testate și securizate.

---

## 6. Care sunt dezavantajele ASP.NET Core Identity?

- **Legat de tehnologie** – presupune că folosești Entity Framework Core și o bază de date relațională. Dacă vrei MongoDB sau alt store, trebuie să implementezi interfețe custom.

- **Schema rigidă** – tabelele generate (`AspNetUsers`, `AspNetRoles` etc.) sunt greu de modificat sau migrat într-un alt sistem. Dacă vrei să muți utilizatorii în altă platformă, e complicat.

- **Nepotrivit pentru API-uri mobile/SPA** – Identity este gândit pentru aplicații server-side cu cookie-uri. Dacă expui un API consumat de Angular sau o aplicație mobilă, ai nevoie de **JWT tokens**, nu cookie-uri de sesiune — ceea ce necesită configurare suplimentară.

- **Overhead** – pentru proiecte mici sau simple, vine cu multă complexitate și tabele care poate nu sunt necesare.

- **Flexibilitate limitată** – dacă vrei un flux de autentificare nestandard (ex. login cu SMS, biometrie), trebuie să extinzi mult dincolo de ce oferă Identity implicit.
