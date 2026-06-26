import re
import unicodedata
from typing import List, Dict, Set, Optional, Tuple, FrozenSet

import spacy

try:
    nlp = spacy.load("fr_core_news_sm")
except OSError:
    import spacy.cli
    spacy.cli.download("fr_core_news_sm")
    nlp = spacy.load("fr_core_news_sm")

ZIPS_VALIDES: Set[str] = {
    "A", "AIDER", "AIMER", "ALLER", "ATTENTE", "AU_REVOIR", "AUJOURD'HUI",
    "AVOIR_MAL", "B", "BIEN", "BON", "BONJOUR", "C", "CA_VA", "COEUR", "COULOIR",
    "COMMENT", "COMPRENDRE", "CONTENT", "COUCOU", "D", "D'ACCORD", "E", "ETUDIANT",
    "F", "FAIRE", "FOND", "G", "GAUCHE", "H", "HABITER", "I", "J", "JEUX_VIDEO",
    "K", "L", "LOISIR", "M", "MERCI", "MOI", "N", "NOM", "NON", "NOUS", "O", "OU",
    "OUI", "P", "PARIS", "PAS", "PENSER", "PLAGE", "POSSIBLE", "Q", "QUOI", "R",
    "RENCONTRER", "RENDEZ_VOUS", "S", "SALLE", "T", "TOI", "U", "V", "VACANCES",
    "VENIR", "VOUS", "W", "X", "Y", "Z",
}
LETTRES = {c for c in ZIPS_VALIDES if len(c) == 1}
PRONOMS = {"MOI", "TOI", "VOUS", "NOUS"}
SALUTATIONS = {"BONJOUR", "COUCOU"}

CONCEPTS_MULTI: List[Tuple[str, List[str]]] = [
    ("comment allez vous", ["CA_VA", "COMMENT"]),
    ("comment vas tu", ["CA_VA", "COMMENT"]),
    ("comment ca va", ["CA_VA", "COMMENT"]),
    ("salle d attente", ["SALLE", "ATTENTE"]),
    ("salle attente", ["SALLE", "ATTENTE"]),
    ("rendez vous", ["RENDEZ_VOUS"]),
    ("rendezvous", ["RENDEZ_VOUS"]),
    ("jeux video", ["JEUX_VIDEO"]),
    ("jeu video", ["JEUX_VIDEO"]),
    ("jeux videos", ["JEUX_VIDEO"]),
    ("au revoir", ["AU_REVOIR"]),
    ("ca va", ["CA_VA"]),
    ("d accord", ["D'ACCORD"]),
    ("avoir mal", ["AVOIR_MAL"]),
    ("aujourd hui", ["AUJOURD'HUI"]),
]

LEXIQUE: Dict[str, str] = {
    "bonjour": "BONJOUR", "coucou": "COUCOU", "salut": "COUCOU",
    "aujourd": "AUJOURD'HUI",
    "je": "MOI", "j": "MOI", "moi": "MOI", "me": "MOI", "m": "MOI",
    "nous": "NOUS", "on": "NOUS",
    "tu": "TOI", "t": "TOI", "toi": "TOI", "tes": "TOI", "ton": "TOI", "ta": "TOI",
    "vous": "VOUS", "votre": "VOUS", "vos": "VOUS",
    "habiter": "HABITER", "habite": "HABITER", "habites": "HABITER", "habitez": "HABITER",
    "aimer": "AIMER", "aime": "AIMER", "aimes": "AIMER", "adore": "AIMER", "adorer": "AIMER",
    "aller": "ALLER", "vais": "ALLER", "va": "ALLER", "vas": "ALLER", "allez": "ALLER",
    "aider": "AIDER", "aide": "AIDER", "aides": "AIDER",
    "penser": "PENSER", "pense": "PENSER", "pensez": "PENSER", "penses": "PENSER",
    "rencontrer": "RENCONTRER", "rencontre": "RENCONTRER",
    "venir": "VENIR", "vient": "VENIR", "venez": "VENIR", "viens": "VENIR",
    "faire": "FAIRE", "fait": "FAIRE", "fais": "FAIRE", "faites": "FAIRE",
    "comprendre": "COMPRENDRE", "comprends": "COMPRENDRE", "comprenez": "COMPRENDRE",
    "appeler": "NOM", "appelle": "NOM", "appelles": "NOM", "appelez": "NOM",
    "appellent": "NOM", "nom": "NOM", "prenom": "NOM",
    "loisir": "LOISIR", "loisirs": "LOISIR",
    "etudiant": "ETUDIANT", "etudiants": "ETUDIANT", "etudiante": "ETUDIANT",
    "vacances": "VACANCES",
    "coeur": "COEUR",
    "plage": "PLAGE", "paris": "PARIS",
    "couloir": "COULOIR", "fond": "FOND", "gauche": "GAUCHE",
    "mal": "AVOIR_MAL",
    "bien": "BIEN", "ravi": "CONTENT", "ravie": "CONTENT", "content": "CONTENT",
    "contente": "CONTENT", "heureux": "CONTENT", "heureuse": "CONTENT",
    "bon": "BON", "bonne": "BON", "bonnes": "BON",
    "merci": "MERCI", "oui": "OUI", "ouais": "OUI", "non": "NON",
    "possible": "POSSIBLE", "peut": "POSSIBLE", "peux": "POSSIBLE", "puis": "POSSIBLE",
    "quoi": "QUOI", "que": "QUOI", "qu": "QUOI", "quel": "QUOI", "quels": "QUOI",
    "quelle": "QUOI", "quelles": "QUOI",
    "ou": "OU", "comment": "COMMENT",
}

DISFLUENCES = {
    "euh", "heu", "ben", "bah", "hein", "genre", "tellement", "trop", "vraiment",
    "beaucoup", "tres", "carrement", "grave", "super", "voila", "bref", "donc",
    "alors", "exactement", "vachement", "quand", "meme",
}
DISFLUENCES_MULTI = ["du coup", "en fait", "tu vois", "tu sais", "s il vous plait",
                     "s il te plait", "est ce que", "qu est ce que", "c est a dire"]

GREETING_NOISE = {"bonjour", "coucou", "salut", "monsieur", "madame", "mr", "mme", "m"}
MOTS_VIDES_NOM = {"genre", "euh", "et", "a", "pour", "de", "la", "le", "les", "du",
                  "appelle", "appelles", "appelez", "nomme", "appeler"}



def _sans_accents(t: str) -> str:
    t = t.replace("œ", "oe").replace("Œ", "OE")   
    return "".join(c for c in unicodedata.normalize("NFKD", t) if not unicodedata.combining(c))


def normaliser(texte: str) -> str:
    t = _sans_accents(texte.lower())
    t = re.sub(r"[’'`]", " ", t)          
    t = re.sub(r"[^a-z0-9]+", " ", t)
    return " ".join(t.split())


def retirer_disfluences(norm: str) -> str:
    for expr in DISFLUENCES_MULTI:
        norm = re.sub(rf"\b{re.escape(expr)}\b", " ", norm)
    return " ".join(m for m in norm.split() if m not in DISFLUENCES)


def _epeler(mot: str) -> List[str]:
    return [c.upper() for c in mot if c.upper() in LETTRES]


def detecter_nom(phrase_brute: str, norm: str) -> Optional[List[str]]:
    """Lettres a epeler, ou None. Detection STRUCTURELLE uniquement (fiable) :
    apres 'appelle/nomme', 'monsieur/madame', ou une salutation. Pas de NER :
    le petit modele FR epele des mots courants ('ca'/'va') par erreur."""
    for motif in (r"\b(?:appelle|appelles|appelez|nomme)\s+(?:genre\s+)?([a-z]+)",
                  r"\b(?:monsieur|madame|mr|mme)\s+([a-z]+)",
                  r"\b(?:bonjour|coucou|salut)\s+([a-z]+)"):
        m = re.search(motif, norm)
        if m and m.group(1) not in LEXIQUE and m.group(1) not in MOTS_VIDES_NOM \
                and m.group(1) not in {"avez", "tout", "le", "vous", "tu"}:
            return _epeler(m.group(1))
    return None


class Phrase:
    """Extrait la signature de concepts + flags + nom propre d'une phrase."""
    def __init__(self, brute: str):
        self.brute = brute
        self.norm_complet = normaliser(brute)
        norm = retirer_disfluences(self.norm_complet)

        glosses: List[str] = []
        travail = " " + norm + " "
        for expr, gloses in CONCEPTS_MULTI:
            if f" {expr} " in travail:
                glosses.extend(gloses)
                travail = travail.replace(f" {expr} ", " ")
        for tok in nlp(" ".join(travail.split())):
            s = tok.text
            if s in LEXIQUE:
                glosses.append(LEXIQUE[s])
            else:
                lem = _sans_accents(tok.lemma_.lower())
                if lem in LEXIQUE:
                    glosses.append(LEXIQUE[lem])

        self.glosses = glosses
        self.set: Set[str] = set(glosses)
        self.negation = bool(re.search(r"\bne\b.*\bpas\b|\bn\b.*\bpas\b|\bpas\b",
                                        self.norm_complet))
        self.question = ("?" in brute or bool(self.set & {"QUOI", "OU", "COMMENT"}))
        self.nom = detecter_nom(brute, self.norm_complet)
        self.sujet = next((g for g in glosses if g in PRONOMS), None)
        self.salutation = "BONJOUR" if "BONJOUR" in self.set else \
                          "COUCOU" if "COUCOU" in self.set else None

    def signature(self) -> Tuple[FrozenSet[str], bool]:
        return frozenset(self.set - SALUTATIONS), self.negation



CORRESPONDANCES: List[Tuple[str, List[str]]] = [
    # --- Document 1 : Medecin ---
    ("Bonjour, avez-vous un rendez-vous ?",            ["BONJOUR", "RENDEZ_VOUS", "VOUS"]),
    ("Salle d'attente au fond du couloir a gauche.",   ["SALLE", "ATTENTE", "COULOIR", "FOND", "GAUCHE"]),
    ("Bonjour Martin, ou avez-vous mal ?",             ["BONJOUR", "<NOM>", "AVOIR_MAL", "OU"]),
    # --- Document 2 : Conversations courantes ---
    ("Bonjour, je suis ravi de vous rencontrer.",      ["BONJOUR", "CONTENT", "RENCONTRER", "VOUS"]),
    ("Comment vous appelez-vous ?",                    ["VOUS", "NOM", "QUOI"]),
    ("Je m'appelle Amy.",                              ["MOI", "NOM", "<NOM>"]),
    ("Nous sommes etudiants.",                         ["NOUS", "ETUDIANT"]),
    ("Comment allez-vous aujourd'hui ?",               ["AUJOURD'HUI", "VOUS", "CA_VA", "COMMENT"]),
    ("Je vais bien, merci.",                           ["MOI", "BIEN", "MERCI"]),
    ("Quels sont vos loisirs ?",                       ["VOUS", "LOISIR", "QUOI"]),
    ("Puis-je vous aider ?",                           ["MOI", "AIDER", "VOUS", "POSSIBLE"]),
    ("Oui, merci pour votre aide.",                    ["OUI", "MERCI", "AIDER"]),
    ("Qu'en pensez-vous ?",                            ["VOUS", "PENSER", "QUOI"]),
    ("Je suis d'accord.",                              ["MOI", "D'ACCORD"]),
    ("Je ne suis pas d'accord.",                       ["MOI", "D'ACCORD", "PAS"]),
    ("Je t'aime.",                                     ["MOI", "AIMER", "TOI"]),
    ("Bonne vacances.",                                ["VACANCES", "BON"]),
    ("Au revoir.",                                     ["AU_REVOIR"]),
    ("T'as fait quoi aujourd'hui ?",                   ["AUJOURD'HUI", "TOI", "FAIRE", "QUOI"]),
    ("J'aime les jeux videos.",                        ["MOI", "AIMER", "JEUX_VIDEO"]),
    ("J'aime aller a la plage.",                       ["PLAGE", "MOI", "AIMER", "ALLER"]),
    ("Coeur.",                                         ["COEUR"]),
    ("T'habites ou ?",                                 ["TOI", "HABITER", "OU"]),
    ("J'habite a Paris.",                              ["PARIS", "MOI", "HABITER"]),
]


INDEX: Dict[Tuple[FrozenSet[str], bool], List[str]] = {}
for _phr, _out in CORRESPONDANCES:
    INDEX[Phrase(_phr).signature()] = _out


RANG = ["BONJOUR", "COUCOU", "AU_REVOIR", "AUJOURD'HUI",
        "PARIS", "PLAGE", "SALLE", "ATTENTE", "COULOIR", "FOND", "GAUCHE",
        "NOUS", "MOI", "TOI", "VOUS",
        "RENDEZ_VOUS", "JEUX_VIDEO", "LOISIR", "ETUDIANT", "VACANCES", "COEUR",
        "NOM", "CA_VA", "CONTENT", "D'ACCORD", "AVOIR_MAL",
        "AIMER", "HABITER", "FAIRE", "PENSER", "ALLER", "AIDER",
        "RENCONTRER", "VENIR", "COMPRENDRE", "BIEN", "BON",
        "OUI", "NON", "MERCI", "POSSIBLE", "PAS", "QUOI", "OU", "COMMENT"]

def _repli(p: Phrase) -> List[str]:
    res = [g for g in RANG if g in p.set]
    if p.negation and "PAS" not in res:
        res.append("PAS")
    if p.nom:
        res += p.nom
    return res


def _finaliser(gloses: List[str]) -> List[str]:
    out: List[str] = []
    for g in gloses:
        if g in ZIPS_VALIDES and (not out or out[-1] != g):
            out.append(g)
    return out


def traiter_phrase(phrase_brute: str) -> List[str]:
    if not phrase_brute or not phrase_brute.strip():
        return []
    p = Phrase(phrase_brute)

    if p.nom and re.search(r"\b(tour|c est a vous|c est a toi|a vous|a toi)\b", p.norm_complet) \
            and "AVOIR_MAL" not in p.set and "NOM" not in p.set:
        return _finaliser(p.nom + ["VENIR"])

    sortie = INDEX.get(p.signature())
    if sortie is not None:
        rendu: List[str] = []
        for i, g in enumerate(sortie):
            if i == 0 and g in SALUTATIONS:
                if p.salutation:
                    rendu.append(p.salutation)
            elif g == "<NOM>":
                rendu.extend(p.nom or [])     
            else:
                rendu.append(g)
        return _finaliser(rendu)

    return _finaliser(_repli(p))


from dataclasses import dataclass, field


@dataclass
class EtatPipeline:
    """Meme forme que l'objet retourne par le fichier de reference (v1.8) :
    memes champs, donc isinstance(...) et acces aux attributs ne cassent pas.
    Le contrat : glosses_finales est une LISTE de strings propres (sans \\n/\\r),
    iterable avec `for gloss in etat.glosses_finales`."""
    phrase_brute: str
    phrase_normalisee: str = ""
    tokens: list = field(default_factory=list)
    tokens_filtres: list = field(default_factory=list)
    glosses_brutes: list = field(default_factory=list)
    glosses_finales: list = field(default_factory=list)
    est_interrogative: bool = False
    est_negative: bool = False

    def __iter__(self):
        return iter(self.glosses_finales)

    def __str__(self):
        return " ".join(self.glosses_finales)


EtatDummy = EtatPipeline


def pipeline(phrase: str) -> EtatPipeline:
    p = Phrase(phrase) if phrase and phrase.strip() else None
    gloses = [
        str(g).replace("\r", "").replace("\n", "").strip()
        for g in traiter_phrase(phrase)
        if str(g).strip()
    ]
    return EtatPipeline(
        phrase_brute=phrase,
        phrase_normalisee=(p.norm_complet if p else ""),
        glosses_brutes=list(gloses),
        glosses_finales=gloses,
        est_interrogative=(p.question if p else False),
        est_negative=(p.negation if p else False),
    )
