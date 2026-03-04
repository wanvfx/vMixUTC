using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using vMixController.ViewModel;

namespace vMixController
{
    /// <summary>
    /// Логика взаимодействия для UTCSplashScreen.xaml
    /// </summary>
    public partial class UTCSplashScreen : UserControl
    {
        char[] symbols = new char[]
        {
            '\ue005','\ue006','\ue00d','\ue012','\ue03f','\ue03f','\ue040','\ue040','\ue041','\ue059',
            '\ue05a','\ue05b','\ue05c','\ue05d','\ue05e','\ue05e','\ue060','\ue060','\ue060','\ue061',
            '\ue062','\ue063','\ue064','\ue065','\ue066','\ue066','\ue067','\ue068','\ue068','\ue069',
            '\ue06a','\ue06b','\ue06c','\ue06d','\ue06e','\ue06f','\ue070','\ue070','\ue071','\ue072',
            '\ue073','\ue074','\ue075','\ue076','\ue085','\ue086','\ue097','\ue098','\ue09a','\ue0a9',
            '\ue0ac','\ue0b4','\ue0b7','\ue0bb','\ue0d8','\ue0df','\ue0e3','\ue0e4','\ue131','\ue139',
            '\ue13a','\ue13b','\ue13c','\ue140','\ue152','\ue163','\ue169','\ue16d','\ue17b','\ue184',
            '\ue185','\ue18f','\ue19a','\ue19b','\ue1a8','\ue1b0','\ue1b0','\ue1bc','\ue1bc','\ue1bc',
            '\ue1c4','\ue1c8','\ue1d3','\ue1d5','\ue1d7','\ue1ed','\ue1f3','\ue1f6','\ue1fe','\ue209',
            '\ue221','\ue222','\ue22d','\ue23d','\ue289','\ue29c','\ue2b7','\ue2bb','\ue2bb','\ue2bb',
            '\ue2c5','\ue2ca','\ue2ca','\ue2cd','\ue2cd','\ue2ce','\ue2ce','\ue2e6','\ue2eb','\ue31e',
            '\ue3af','\ue3af','\ue3b1','\ue3b2','\ue3f5','\ue43c','\ue445','\ue447','\ue448','\ue46c',
            '\ue473','\ue476','\ue477','\ue47a','\ue47b','\ue47b','\ue490','\ue494','\ue4a5','\ue4a8',
            '\ue4a9','\ue4aa','\ue4ab','\ue4ac','\ue4ad','\ue4af','\ue4b0','\ue4b3','\ue4b5','\ue4b6',
            '\ue4b7','\ue4b8','\ue4b9','\ue4ba','\ue4bb','\ue4bc','\ue4bd','\ue4be','\ue4bf','\ue4c0',
            '\ue4c1','\ue4c2','\ue4c3','\ue4c4','\ue4c5','\ue4c6','\ue4c7','\ue4c8','\ue4c9','\ue4ca',
            '\ue4cb','\ue4cc','\ue4ce','\ue4cf','\ue4d0','\ue4d1','\ue4d2','\ue4d3','\ue4d4','\ue4d5',
            '\ue4d6','\ue4d7','\ue4d8','\ue4d9','\ue4da','\ue4db','\ue4dc','\ue4dd','\ue4de','\ue4e0',
            '\ue4e0','\ue4e1','\ue4e2','\ue4e3','\ue4e4','\ue4e5','\ue4e6','\ue4e8','\ue4e9','\ue4ea',
            '\ue4eb','\ue4ed','\ue4ef','\ue4f0','\ue4f1','\ue4f2','\ue4f3','\ue4f4','\ue4f5','\ue4f6',
            '\ue4f7','\ue4f8','\ue4f9','\ue4fa','\ue4fb','\ue4fc','\ue4fd','\ue4fe','\ue4ff','\ue500',
            '\ue501','\ue502','\ue503','\ue507','\ue508','\ue509','\ue50a','\ue50b','\ue50c','\ue50d',
            '\ue50e','\ue50f','\ue510','\ue511','\ue512','\ue513','\ue514','\ue515','\ue516','\ue517',
            '\ue518','\ue519','\ue51a','\ue51b','\ue51c','\ue51d','\ue51e','\ue51f','\ue520','\ue521',
            '\ue522','\ue523','\ue524','\ue525','\ue527','\ue528','\ue529','\ue52a','\ue52b','\ue52c',
            '\ue52d','\ue52e','\ue52f','\ue532','\ue533','\ue534','\ue535','\ue536','\ue537','\ue538',
            '\ue539','\ue53a','\ue53b','\ue53c','\ue53d','\ue53e','\ue53f','\ue540','\ue541','\ue542',
            '\ue543','\ue544','\ue545','\ue546','\ue547','\ue548','\ue549','\ue54a','\ue54b','\ue54c',
            '\ue54d','\ue54e','\ue54f','\ue551','\ue552','\ue553','\ue554','\ue555','\ue556','\ue557',
            '\ue558','\ue55a','\ue55b','\ue55c','\ue55d','\ue55e','\ue55f','\ue560','\ue561','\ue562',
            '\ue563','\ue564','\ue565','\ue566','\ue567','\ue568','\ue569','\ue56a','\ue56b','\ue56c',
            '\ue56d','\ue56e','\ue56f','\ue571','\ue572','\ue573','\ue574','\ue576','\ue577','\ue578',
            '\ue579','\ue579','\ue579','\ue579','\ue57a','\ue57b','\ue57c','\ue57d','\ue57e','\ue57f',
            '\ue580','\ue581','\ue582','\ue583','\ue584','\ue585','\ue586','\ue587','\ue589','\ue58a',
            '\ue58b','\ue58c','\ue58d','\ue58e','\ue58f','\ue591','\ue592','\ue593','\ue594','\ue595',
            '\ue596','\ue597','\ue598','\ue599','\ue59a','\ue59c','\ue59d','\ue5a0','\ue5a1','\ue5a9',
            '\ue5aa','\ue5af','\ue5b4','\ue678','\ue67a','\ue682','\ue68f','\ue68f','\ue691','\ue695',
            '\ue696','\ue697','\ue698','\ue699','\ue69a','\ue69b','\ue790','\ue807','\ue80a','\ue816',
            '\ue81b','\ue81c','\ue81d','\ue820','\ue820','\uf000','\uf000','\uf001','\uf002','\uf002',
            '\uf004','\uf005','\uf007','\uf007','\uf007','\uf008','\uf008','\uf008','\uf009','\uf009',
            '\uf00a','\uf00a','\uf00b','\uf00b','\uf00c','\uf00d','\uf00d','\uf00d','\uf00d','\uf00d',
            '\uf00e','\uf00e','\uf010','\uf010','\uf011','\uf012','\uf012','\uf012','\uf013','\uf013',
            '\uf015','\uf015','\uf015','\uf015','\uf017','\uf017','\uf018','\uf019','\uf01c','\uf01e',
            '\uf01e','\uf01e','\uf01e','\uf021','\uf021','\uf021','\uf022','\uf022','\uf023','\uf024',
            '\uf025','\uf025','\uf025','\uf026','\uf027','\uf027','\uf028','\uf028','\uf029','\uf02a',
            '\uf02b','\uf02c','\uf02d','\uf02e','\uf02f','\uf030','\uf030','\uf031','\uf032','\uf033',
            '\uf034','\uf035','\uf036','\uf037','\uf038','\uf039','\uf03a','\uf03a','\uf03b','\uf03b',
            '\uf03c','\uf03d','\uf03d','\uf03e','\uf041','\uf041','\uf042','\uf042','\uf043','\uf043',
            '\uf044','\uf044','\uf047','\uf047','\uf048','\uf048','\uf049','\uf049','\uf04a','\uf04b',
            '\uf04c','\uf04d','\uf04e','\uf050','\uf050','\uf051','\uf051','\uf052','\uf053','\uf054',
            '\uf055','\uf055','\uf056','\uf056','\uf057','\uf057','\uf057','\uf058','\uf058','\uf059',
            '\uf059','\uf05a','\uf05a','\uf05b','\uf05e','\uf05e','\uf060','\uf061','\uf062','\uf063',
            '\uf064','\uf064','\uf065','\uf066','\uf068','\uf068','\uf06a','\uf06a','\uf06b','\uf06c',
            '\uf06d','\uf06e','\uf070','\uf071','\uf071','\uf071','\uf072','\uf073','\uf073','\uf074',
            '\uf074','\uf075','\uf076','\uf077','\uf078','\uf079','\uf07a','\uf07a','\uf07b','\uf07b',
            '\uf07c','\uf07d','\uf07d','\uf07e','\uf07e','\uf080','\uf080','\uf083','\uf084','\uf085',
            '\uf085','\uf086','\uf089','\uf08b','\uf08b','\uf08d','\uf08d','\uf08e','\uf08e','\uf090',
            '\uf090','\uf091','\uf093','\uf094','\uf095','\uf098','\uf098','\uf09c','\uf09d','\uf09d',
            '\uf09e','\uf09e','\uf0a0','\uf0a0','\uf0a1','\uf0a3','\uf0a4','\uf0a5','\uf0a6','\uf0a7',
            '\uf0a8','\uf0a8','\uf0a9','\uf0a9','\uf0aa','\uf0aa','\uf0ab','\uf0ab','\uf0ac','\uf0ad',
            '\uf0ae','\uf0ae','\uf0b0','\uf0b1','\uf0b2','\uf0b2','\uf0c0','\uf0c1','\uf0c1','\uf0c2',
            '\uf0c3','\uf0c4','\uf0c4','\uf0c5','\uf0c6','\uf0c7','\uf0c7','\uf0c8','\uf0c9','\uf0c9',
            '\uf0ca','\uf0ca','\uf0cb','\uf0cb','\uf0cb','\uf0cc','\uf0cd','\uf0ce','\uf0d0','\uf0d0',
            '\uf0d1','\uf0d6','\uf0d7','\uf0d8','\uf0d9','\uf0da','\uf0db','\uf0db','\uf0dc','\uf0dc',
            '\uf0dd','\uf0dd','\uf0de','\uf0de','\uf0e0','\uf0e2','\uf0e2','\uf0e2','\uf0e2','\uf0e2',
            '\uf0e3','\uf0e3','\uf0e7','\uf0e7','\uf0e8','\uf0e9','\uf0ea','\uf0ea','\uf0eb','\uf0ec',
            '\uf0ec','\uf0ed','\uf0ed','\uf0ed','\uf0ee','\uf0ee','\uf0ee','\uf0f0','\uf0f0','\uf0f1',
            '\uf0f2','\uf0f3','\uf0f4','\uf0f4','\uf0f8','\uf0f8','\uf0f8','\uf0f9','\uf0f9','\uf0fa',
            '\uf0fa','\uf0fb','\uf0fb','\uf0fc','\uf0fc','\uf0fd','\uf0fd','\uf0fe','\uf0fe','\uf100',
            '\uf100','\uf101','\uf101','\uf102','\uf102','\uf103','\uf103','\uf104','\uf105','\uf106',
            '\uf107','\uf109','\uf10a','\uf10b','\uf10d','\uf10d','\uf10e','\uf10e','\uf110','\uf111',
            '\uf118','\uf118','\uf119','\uf119','\uf11a','\uf11a','\uf11b','\uf11c','\uf11e','\uf120',
            '\uf121','\uf122','\uf122','\uf124','\uf125','\uf126','\uf127','\uf127','\uf127','\uf127',
            '\uf129','\uf12b','\uf12c','\uf12d','\uf12e','\uf130','\uf131','\uf132','\uf132','\uf133',
            '\uf134','\uf135','\uf137','\uf137','\uf138','\uf138','\uf139','\uf139','\uf13a','\uf13a',
            '\uf13d','\uf13e','\uf13e','\uf140','\uf141','\uf141','\uf142','\uf142','\uf143','\uf143',
            '\uf144','\uf144','\uf145','\uf146','\uf146','\uf148','\uf148','\uf149','\uf149','\uf14a',
            '\uf14a','\uf14b','\uf14b','\uf14b','\uf14c','\uf14c','\uf14d','\uf14d','\uf14e','\uf150',
            '\uf150','\uf151','\uf151','\uf152','\uf152','\uf153','\uf153','\uf153','\uf154','\uf154',
            '\uf154','\uf156','\uf156','\uf157','\uf157','\uf157','\uf157','\uf157','\uf158','\uf158',
            '\uf158','\uf158','\uf159','\uf159','\uf159','\uf15b','\uf15c','\uf15c','\uf15c','\uf15d',
            '\uf15d','\uf15d','\uf15e','\uf15e','\uf160','\uf160','\uf160','\uf161','\uf161','\uf162',
            '\uf162','\uf162','\uf163','\uf163','\uf164','\uf165','\uf175','\uf175','\uf176','\uf176',
            '\uf177','\uf177','\uf178','\uf178','\uf182','\uf182','\uf183','\uf183','\uf185','\uf186',
            '\uf187','\uf187','\uf188','\uf191','\uf191','\uf192','\uf192','\uf193','\uf195','\uf197',
            '\uf197','\uf199','\uf199','\uf19c','\uf19c','\uf19c','\uf19c','\uf19c','\uf19d','\uf19d',
            '\uf1ab','\uf1ac','\uf1ad','\uf1ae','\uf1b0','\uf1b2','\uf1b3','\uf1b8','\uf1b9','\uf1b9',
            '\uf1ba','\uf1ba','\uf1bb','\uf1c0','\uf1c1','\uf1c2','\uf1c3','\uf1c4','\uf1c5','\uf1c6',
            '\uf1c6','\uf1c7','\uf1c8','\uf1c9','\uf1cd','\uf1ce','\uf1d8','\uf1da','\uf1da','\uf1dc',
            '\uf1dc','\uf1dd','\uf1de','\uf1de','\uf1e0','\uf1e0','\uf1e1','\uf1e1','\uf1e2','\uf1e3',
            '\uf1e3','\uf1e3','\uf1e4','\uf1e4','\uf1e5','\uf1e6','\uf1ea','\uf1eb','\uf1eb','\uf1eb',
            '\uf1ec','\uf1f6','\uf1f8','\uf1f9','\uf1fb','\uf1fb','\uf1fb','\uf1fc','\uf1fc','\uf1fd',
            '\uf1fd','\uf1fd','\uf1fe','\uf1fe','\uf200','\uf200','\uf201','\uf201','\uf204','\uf205',
            '\uf206','\uf207','\uf20a','\uf20b','\uf20b','\uf20b','\uf20b','\uf20b','\uf217','\uf218',
            '\uf219','\uf21a','\uf21b','\uf21c','\uf21d','\uf21e','\uf21e','\uf221','\uf222','\uf223',
            '\uf224','\uf225','\uf225','\uf226','\uf227','\uf228','\uf229','\uf22a','\uf22a','\uf22b',
            '\uf22b','\uf22c','\uf22d','\uf233','\uf234','\uf235','\uf235','\uf236','\uf238','\uf239',
            '\uf239','\uf240','\uf240','\uf240','\uf241','\uf241','\uf242','\uf242','\uf243','\uf243',
            '\uf244','\uf244','\uf245','\uf245','\uf246','\uf247','\uf248','\uf249','\uf249','\uf24d',
            '\uf24e','\uf24e','\uf251','\uf251','\uf252','\uf252','\uf253','\uf253','\uf254','\uf254',
            '\uf255','\uf255','\uf256','\uf256','\uf257','\uf258','\uf259','\uf25a','\uf25b','\uf25c',
            '\uf25d','\uf26c','\uf26c','\uf26c','\uf271','\uf272','\uf273','\uf273','\uf274','\uf275',
            '\uf276','\uf277','\uf277','\uf279','\uf27a','\uf27a','\uf28b','\uf28b','\uf28d','\uf28d',
            '\uf290','\uf290','\uf291','\uf291','\uf29a','\uf29d','\uf29d','\uf29e','\uf2a0','\uf2a0',
            '\uf2a1','\uf2a2','\uf2a2','\uf2a3','\uf2a3','\uf2a3','\uf2a3','\uf2a4','\uf2a4','\uf2a4',
            '\uf2a4','\uf2a7','\uf2a7','\uf2a7','\uf2a8','\uf2a8','\uf2b4','\uf2b4','\uf2b4','\uf2b5',
            '\uf2b5','\uf2b5','\uf2b6','\uf2b9','\uf2b9','\uf2bb','\uf2bb','\uf2bb','\uf2bd','\uf2bd',
            '\uf2c1','\uf2c2','\uf2c2','\uf2c7','\uf2c7','\uf2c7','\uf2c7','\uf2c8','\uf2c8','\uf2c8',
            '\uf2c8','\uf2c9','\uf2c9','\uf2c9','\uf2c9','\uf2ca','\uf2ca','\uf2ca','\uf2ca','\uf2cb',
            '\uf2cb','\uf2cb','\uf2cb','\uf2cc','\uf2cd','\uf2cd','\uf2ce','\uf2d0','\uf2d1','\uf2d2',
            '\uf2d3','\uf2d3','\uf2d3','\uf2db','\uf2dc','\uf2e5','\uf2e5','\uf2e7','\uf2e7','\uf2ea',
            '\uf2ea','\uf2ea','\uf2ea','\uf2ed','\uf2ed','\uf2f1','\uf2f1','\uf2f2','\uf2f5','\uf2f5',
            '\uf2f6','\uf2f6','\uf2f9','\uf2f9','\uf2f9','\uf2fe','\uf302','\uf303','\uf303','\uf304',
            '\uf305','\uf305','\uf306','\uf309','\uf309','\uf30a','\uf30a','\uf30b','\uf30b','\uf30c',
            '\uf30c','\uf312','\uf31c','\uf31c','\uf31e','\uf31e','\uf328','\uf337','\uf337','\uf338',
            '\uf338','\uf34e','\uf358','\uf358','\uf359','\uf359','\uf35a','\uf35a','\uf35b','\uf35b',
            '\uf35d','\uf35d','\uf360','\uf360','\uf362','\uf362','\uf363','\uf386','\uf387','\uf390',
            '\uf390','\uf3a5','\uf3be','\uf3be','\uf3bf','\uf3bf','\uf3c1','\uf3c5','\uf3c5','\uf3c9',
            '\uf3c9','\uf3cd','\uf3cd','\uf3ce','\uf3ce','\uf3ce','\uf3cf','\uf3cf','\uf3d1','\uf3d1',
            '\uf3dd','\uf3e0','\uf3e0','\uf3e5','\uf3e5','\uf3ed','\uf3ed','\uf3fa','\uf3fa','\uf3fb',
            '\uf3fb','\uf3ff','\uf3ff','\uf410','\uf410','\uf410','\uf410','\uf422','\uf422','\uf424',
            '\uf424','\uf432','\uf433','\uf433',
            '\uf434','\uf434','\uf436','\uf439','\uf43a','\uf43c','\uf43f','\uf441','\uf443','\uf445',
            '\uf447','\uf44b','\uf44e','\uf44e','\uf450','\uf450','\uf453','\uf458','\uf458','\uf458',
            '\uf45c','\uf45d','\uf45d','\uf45d','\uf45f','\uf45f','\uf461','\uf461','\uf462','\uf462',
            '\uf466','\uf468','\uf468','\uf468','\uf469','\uf46a','\uf46a','\uf46b','\uf46c','\uf46d',
            '\uf470','\uf470','\uf471','\uf472','\uf472','\uf474','\uf474','\uf477','\uf478','\uf478',
            '\uf479','\uf479','\uf47e','\uf47e','\uf47f','\uf47f','\uf481','\uf482','\uf484','\uf485',
            '\uf486','\uf486','\uf487','\uf487','\uf48b','\uf48b','\uf48d','\uf48e','\uf490','\uf491',
            '\uf492','\uf493','\uf494','\uf496','\uf496','\uf497','\uf49e','\uf4ad','\uf4ad','\uf4b3',
            '\uf4b8','\uf4b9','\uf4b9','\uf4ba','\uf4bd','\uf4be','\uf4c0','\uf4c0','\uf4c1','\uf4c1',
            '\uf4c2','\uf4c4','\uf4c4','\uf4cd','\uf4ce','\uf4ce','\uf4d3','\uf4d6','\uf4d7','\uf4d8',
            '\uf4d8','\uf4d9','\uf4d9','\uf4da','\uf4da','\uf4db','\uf4de','\uf4de','\uf4df','\uf4e2',
            '\uf4e3','\uf4fb','\uf4fc','\uf4fd','\uf4fe','\uf4fe','\uf4ff','\uf4ff','\uf500','\uf500',
            '\uf501','\uf502','\uf503','\uf504','\uf505','\uf506','\uf506','\uf506','\uf507','\uf508',
            '\uf509','\uf509','\uf515','\uf515','\uf516','\uf516','\uf517','\uf518','\uf519','\uf519',
            '\uf51a','\uf51b','\uf51b','\uf51c','\uf51c','\uf51d','\uf51e','\uf51f','\uf520','\uf521',
            '\uf522','\uf523','\uf524','\uf525','\uf526','\uf527','\uf528','\uf529','\uf52a','\uf52b',
            '\uf52d','\uf52e','\uf52f','\uf530','\uf532','\uf533','\uf534','\uf535','\uf537','\uf538',
            '\uf539','\uf539','\uf53a','\uf53b','\uf53b','\uf53c','\uf53d','\uf53d','\uf53e','\uf53f',
            '\uf540','\uf540','\uf542','\uf542','\uf543','\uf544','\uf545','\uf546','\uf547','\uf548',
            '\uf549','\uf54a','\uf54b','\uf54c','\uf54d','\uf54d','\uf54e','\uf54f','\uf54f','\uf550',
            '\uf550','\uf550','\uf551','\uf552','\uf553','\uf553','\uf553','\uf554','\uf554','\uf555',
            '\uf556','\uf556','\uf557','\uf558','\uf558','\uf559','\uf55a','\uf55a','\uf55b','\uf55c',
            '\uf55d','\uf55e','\uf55e','\uf55f','\uf560','\uf561','\uf561','\uf562','\uf562','\uf563',
            '\uf564','\uf565','\uf565','\uf566','\uf566','\uf567','\uf567','\uf568','\uf568','\uf569',
            '\uf56a','\uf56b','\uf56b','\uf56c','\uf56d','\uf56d','\uf56e','\uf56e','\uf56f','\uf56f',
            '\uf570','\uf571','\uf572','\uf573','\uf574','\uf574','\uf575','\uf576','\uf577','\uf578',
            '\uf579','\uf579','\uf57a','\uf57a','\uf57b','\uf57b','\uf57c','\uf57c','\uf57d','\uf57d',
            '\uf57d','\uf57d','\uf57e','\uf57e','\uf57f','\uf57f','\uf580','\uf580','\uf581','\uf581',
            '\uf582','\uf582','\uf583','\uf583','\uf584','\uf584','\uf585','\uf585','\uf586','\uf586',
            '\uf587','\uf587','\uf588','\uf588','\uf589','\uf589','\uf58a','\uf58a','\uf58b','\uf58b',
            '\uf58c','\uf58c','\uf58d','\uf58d','\uf58d','\uf58e','\uf58e','\uf590','\uf591','\uf593',
            '\uf593','\uf594','\uf595','\uf596','\uf596','\uf597','\uf597','\uf598','\uf598','\uf599',
            '\uf599','\uf59a','\uf59a','\uf59b','\uf59b','\uf59c','\uf59c','\uf59d','\uf59d','\uf59f',
            '\uf59f','\uf5a0','\uf5a0','\uf5a1','\uf5a2','\uf5a4','\uf5a4','\uf5a5','\uf5a5','\uf5a6',
            '\uf5a7','\uf5aa','\uf5ab','\uf5ac','\uf5ad','\uf5ae','\uf5ae','\uf5af','\uf5b0','\uf5b1',
            '\uf5b3','\uf5b3','\uf5b4','\uf5b4','\uf5b6','\uf5b6','\uf5b7','\uf5b8','\uf5b8','\uf5ba',
            '\uf5bb','\uf5bc','\uf5bd','\uf5bf','\uf5c0','\uf5c0','\uf5c1','\uf5c2','\uf5c2','\uf5c3',
            '\uf5c4','\uf5c4','\uf5c5','\uf5c5','\uf5c5','\uf5c7','\uf5c7','\uf5c8','\uf5c8','\uf5c9',
            '\uf5ca','\uf5cd','\uf5ce','\uf5ce','\uf5d0','\uf5d0','\uf5d1','\uf5d1','\uf5d2','\uf5d7',
            '\uf5da','\uf5da','\uf5dc','\uf5de','\uf5de','\uf5df','\uf5df','\uf5e1','\uf5e1','\uf5e4',
            '\uf5e7','\uf5eb','\uf5eb','\uf5ee','\uf5ee','\uf5fc','\uf5fd','\uf601','\uf601','\uf604',
            '\uf610','\uf613','\uf619','\uf61f','\uf61f','\uf621','\uf624','\uf624','\uf624','\uf624',
            '\uf625','\uf625','\uf625','\uf629','\uf629','\uf629','\uf62a','\uf62a','\uf62a','\uf62e',
            '\uf62f','\uf630','\uf630','\uf637','\uf63b','\uf63c','\uf641','\uf641','\uf644','\uf647',
            '\uf647','\uf64a','\uf64a','\uf64f','\uf651','\uf653','\uf654','\uf655','\uf658','\uf65d',
            '\uf65e','\uf662','\uf662','\uf664','\uf665','\uf666','\uf666','\uf669','\uf66a','\uf66a',
            '\uf66b','\uf66d','\uf66f','\uf674','\uf674','\uf676','\uf678','\uf679','\uf67b','\uf67b',
            '\uf67c','\uf67f','\uf681','\uf681','\uf682','\uf682','\uf683','\uf683','\uf684','\uf684',
            '\uf687','\uf687','\uf688','\uf688','\uf689','\uf689','\uf696','\uf698','\uf698','\uf699',
            '\uf69a','\uf69b','\uf6a0','\uf6a0','\uf6a1','\uf6a7','\uf6a9','\uf6a9','\uf6a9','\uf6ad',
            '\uf6b6','\uf6b7','\uf6b7','\uf6bb','\uf6be','\uf6c0','\uf6c3','\uf6c4','\uf6c8','\uf6cf',
            '\uf6d1','\uf6d3','\uf6d5','\uf6d7','\uf6d9','\uf6dd','\uf6de','\uf6de','\uf6e2','\uf6e3',
            '\uf6e6','\uf6e8','\uf6ec','\uf6ec','\uf6ed','\uf6f0','\uf6f1','\uf6f1','\uf6f2','\uf6f2',
            '\uf6fa','\uf6fc','\uf6ff','\uf700','\uf70b','\uf70c','\uf70c','\uf70e','\uf714','\uf715',
            '\uf717','\uf71e','\uf71e','\uf71e','\uf722','\uf728','\uf729','\uf72b','\uf72e','\uf72f',
            '\uf73b','\uf73c','\uf73d','\uf740','\uf743','\uf747','\uf74d','\uf751','\uf752','\uf752',
            '\uf753','\uf756','\uf75a','\uf75a','\uf75b','\uf75e','\uf75f','\uf769','\uf76b','\uf76c',
            '\uf76c','\uf76f','\uf770','\uf772','\uf772','\uf773','\uf77c','\uf77d','\uf77d','\uf780',
            '\uf781','\uf783','\uf784','\uf786','\uf787','\uf788','\uf78c','\uf78c','\uf793','\uf794',
            '\uf796','\uf79c','\uf79f','\uf79f','\uf7a0','\uf7a0','\uf7a2','\uf7a2','\uf7a4','\uf7a5',
            '\uf7a6','\uf7a9','\uf7a9','\uf7aa','\uf7ab','\uf7ad','\uf7ae','\uf7b5','\uf7b6','\uf7b9',
            '\uf7ba','\uf7ba','\uf7bd','\uf7bf','\uf7c0','\uf7c2','\uf7c4','\uf7c5','\uf7c5','\uf7c9',
            '\uf7c9','\uf7ca','\uf7ca','\uf7cc','\uf7cd','\uf7cd','\uf7ce','\uf7ce','\uf7d0','\uf7d2',
            '\uf7d7','\uf7d7','\uf7d8','\uf7d9','\uf7d9','\uf7da','\uf7da','\uf7e4','\uf7e4','\uf7e5',
            '\uf7e6','\uf7ec','\uf7ef','\uf7f2','\uf7f2','\uf7f3','\uf7f5','\uf7f7','\uf7fa','\uf7fb',
            '\uf802','\uf805','\uf805','\uf806','\uf807','\uf807','\uf807','\uf80d','\uf80f','\uf810',
            '\uf812','\uf815','\uf816','\uf818','\uf81d','\uf827','\uf827','\uf828','\uf828','\uf829',
            '\uf829','\uf82a','\uf82a','\uf82f','\uf83e','\uf84a','\uf84a','\uf84c','\uf850','\uf853',
            '\uf853','\uf85e','\uf85e','\uf863','\uf86d','\uf86d','\uf879','\uf879','\uf87b','\uf87b',
            '\uf87c','\uf87c','\uf87d','\uf87d','\uf881','\uf881','\uf881','\uf882','\uf882','\uf884',
            '\uf884','\uf884','\uf885','\uf885','\uf886','\uf886','\uf886','\uf887','\uf887','\uf891',
            '\uf897','\uf8c0','\uf8c1','\uf8cc','\uf8cc','\uf8d7','\uf8d9','\uf8ef','\uf8ff',
            '\ue057','\ue077','\ue078','\ue079','\ue07a','\ue07b','\ue07c','\ue07d','\ue07e','\ue07f',
            '\ue080','\ue080','\ue081','\ue082','\ue083','\ue084','\ue087','\ue088','\ue2d0','\ue2d0',
            '\ue340','\ue360','\ue3d9','\ue40f','\ue43a','\ue44a','\ue499','\ue49b','\ue4a0','\ue530',
            '\ue531','\ue570','\ue5ac','\ue5ad','\ue5ae','\ue5ae','\ue5c6','\ue5c7','\ue60b','\ue60c',
            '\ue618','\ue619','\ue61a','\ue61b','\ue62b','\ue62d','\ue62e','\ue62f','\ue63b','\ue63c',
            '\ue63d','\ue640','\ue641','\ue65c','\ue663','\ue671','\ue67b','\ue67c','\ue682','\ue683',
            '\ue684','\ue693','\ue694','\ue69f','\ue6a2','\ue6a3','\ue7cf','\ue7d0','\ue7d4','\ue7d5',
            '\ue7d6','\ue7d6','\ue7d7','\ue7d8','\ue7d9','\ue7da','\ue7db','\ue7dc','\ue7dd','\ue7de',
            '\ue7e2','\ue7e3','\ue7e4','\ue7ff','\ue812','\uf081','\uf081','\uf082','\uf082','\uf08c',
            '\uf092','\uf092','\uf099','\uf09a','\uf09b','\uf0d2','\uf0d3','\uf0d3','\uf0d4','\uf0d4',
            '\uf0d5','\uf0e1','\uf113','\uf136','\uf13b','\uf13c','\uf15a','\uf167','\uf168','\uf169',
            '\uf169','\uf16b','\uf16c','\uf16d','\uf16e','\uf170','\uf171','\uf173','\uf174','\uf174',
            '\uf179','\uf17a','\uf17b','\uf17c','\uf17d','\uf17e','\uf180','\uf181','\uf184','\uf189',
            '\uf18a','\uf18b','\uf18c','\uf18d','\uf194','\uf194','\uf198','\uf198','\uf19a','\uf19b',
            '\uf19e','\uf1a0','\uf1a1','\uf1a2','\uf1a2','\uf1a3','\uf1a4','\uf1a5','\uf1a6','\uf1a7',
            '\uf1a8','\uf1a9','\uf1aa','\uf1b4','\uf1b5','\uf1b5','\uf1b6','\uf1b7','\uf1b7','\uf1bc',
            '\uf1bd','\uf1be','\uf1ca','\uf1cb','\uf1cc','\uf1d0','\uf1d1','\uf1d2','\uf1d2','\uf1d3',
            '\uf1d4','\uf1d5','\uf1d6','\uf1d7','\uf1e7','\uf1e8','\uf1e9','\uf1ed','\uf1ee','\uf1f0',
            '\uf1f1','\uf1f2','\uf1f3','\uf1f4','\uf1f5','\uf202','\uf203','\uf203','\uf208','\uf209',
            '\uf20d','\uf20e','\uf210','\uf211','\uf212','\uf213','\uf214','\uf215','\uf216','\uf231',
            '\uf232','\uf237','\uf23a','\uf23a','\uf23b','\uf23c','\uf23d','\uf23e','\uf24b','\uf24c',
            '\uf25e','\uf260','\uf261','\uf263','\uf264','\uf264','\uf265','\uf266','\uf267','\uf268',
            '\uf269','\uf26a','\uf26b','\uf26d','\uf26e','\uf270','\uf27c','\uf27d','\uf27e','\uf280',
            '\uf281','\uf282','\uf284','\uf285','\uf286','\uf287','\uf288','\uf289','\uf28a','\uf293',
            '\uf294','\uf296','\uf297','\uf298','\uf299','\uf2a5','\uf2a6','\uf2a9','\uf2aa','\uf2aa',
            '\uf2ab','\uf2ab','\uf2ad','\uf2ad','\uf2ae','\uf2b0','\uf2b1','\uf2b2','\uf2b3','\uf2b4',
            '\uf2b4','\uf2b4','\uf2b8','\uf2c4','\uf2c5','\uf2c6','\uf2c6','\uf2d5','\uf2d6','\uf2d7',
            '\uf2d8','\uf2d9','\uf2da','\uf2dd','\uf2de','\uf2e0','\uf35c','\uf35c','\uf368','\uf369',
            '\uf36a','\uf36b','\uf36c','\uf36d','\uf36e','\uf36f','\uf370','\uf371','\uf372','\uf373',
            '\uf374','\uf375','\uf378','\uf379','\uf37a','\uf37b','\uf37c','\uf37d','\uf37f','\uf380',
            '\uf383','\uf384','\uf385','\uf388','\uf38b','\uf38c','\uf38d','\uf38e','\uf38f','\uf391',
            '\uf392','\uf393','\uf394','\uf395','\uf396','\uf397','\uf397','\uf399','\uf39a','\uf39d',
            '\uf39e','\uf39f','\uf3a1','\uf3a2','\uf3a3','\uf3a4','\uf3a6','\uf3a7','\uf3a8','\uf3a9',
            '\uf3aa','\uf3ab','\uf3ac','\uf3ad','\uf3ae','\uf3af','\uf3af','\uf3b0','\uf3b1','\uf3b2',
            '\uf3b4','\uf3b5','\uf3b6','\uf3b7','\uf3b8','\uf3b9','\uf3b9','\uf3ba','\uf3bb','\uf3bb',
            '\uf3bc','\uf3bd','\uf3c0','\uf3c3','\uf3c4','\uf3c6','\uf3c8','\uf3ca','\uf3cb','\uf3cc',
            '\uf3d0','\uf3d2','\uf3d3','\uf3d4','\uf3d5','\uf3d6','\uf3d7','\uf3d8','\uf3d9','\uf3da',
            '\uf3db','\uf3dc','\uf3df','\uf3e1','\uf3e2','\uf3e3','\uf3e4','\uf3e4','\uf3e6','\uf3e7',
            '\uf3e8','\uf3e9','\uf3ea','\uf3eb','\uf3ec','\uf3ee','\uf3f3','\uf3f5','\uf3f6','\uf3f7',
            '\uf3f8','\uf3f9','\uf402','\uf403','\uf404','\uf405','\uf407','\uf408','\uf409','\uf40a',
            '\uf40b','\uf40c','\uf40c','\uf40d','\uf411','\uf412','\uf413','\uf414','\uf415','\uf416',
            '\uf417','\uf419','\uf41a','\uf41b','\uf41c','\uf41d','\uf41e','\uf41f','\uf420','\uf421',
            '\uf423','\uf426','\uf427','\uf428','\uf429','\uf42a','\uf42b','\uf42c','\uf42d','\uf42e',
            '\uf42f','\uf430','\uf431','\uf431','\uf44d','\uf452','\uf457','\uf459','\uf4d5','\uf4e4',
            '\uf4e5','\uf4e7','\uf4e8','\uf4e9','\uf4ea','\uf4eb','\uf4ec','\uf4ed','\uf4ee','\uf4ef',
            '\uf4f0','\uf4f1','\uf4f2','\uf4f3','\uf4f4','\uf4f5','\uf4f6','\uf4f7','\uf4f8','\uf4f9',
            '\uf50a','\uf50b','\uf50c','\uf50d','\uf50e','\uf50f','\uf510',
            '\uf511','\uf512','\uf513','\uf514','\uf592','\uf59e','\uf5a3','\uf5a8','\uf5b2','\uf5b5',
            '\uf5be','\uf5c6','\uf5cc','\uf5cf','\uf5f1','\uf5f7','\uf5fa','\uf60f','\uf612','\uf63f',
            '\uf642','\uf69d','\uf6c9','\uf6ca','\uf6cc','\uf6dc','\uf730','\uf731','\uf75d','\uf77a',
            '\uf77b','\uf785','\uf789','\uf78d','\uf790','\uf791','\uf797','\uf798','\uf799','\uf7af',
            '\uf7b0','\uf7b1','\uf7b3','\uf7bb','\uf7bc','\uf7c6','\uf7d3','\uf7d6','\uf7df','\uf7e0',
            '\uf7e1','\uf7e3','\uf834','\uf835','\uf836','\uf837','\uf838','\uf839','\uf83a','\uf83b',
            '\uf83c','\uf83d','\uf83f','\uf840','\uf841','\uf842','\uf89e','\uf8a6','\uf8ca','\uf8d2',
            '\uf8e1','\uf8e8'
                    };

        public static RenderTargetBitmap RenderCharacterToBitmap(
        char character,
        double fontSize = 72,
        Brush foreground = null,
        Size? renderSize = null)
        {
            // Default values
            foreground = foreground ?? Brushes.White;
            renderSize = renderSize ?? new Size(fontSize, fontSize);

            // Create a visual to render
            var textBlock = new TextBlock
            {
                Text = character.ToString(),
                FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "/vMixControllerSkin;Component/#Font Awesome 7 Free Solid"),
                FontSize = fontSize,
                Foreground = foreground,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Arrange the text block
            textBlock.Measure(renderSize.Value);
            textBlock.Arrange(new Rect(renderSize.Value));

            // Render to bitmap
            var renderTarget = new RenderTargetBitmap(
                (int)renderSize.Value.Width,
                (int)renderSize.Value.Height,
                96, 96, PixelFormats.Pbgra32);
            renderTarget.Clear();
            renderTarget.Render(textBlock);

            return renderTarget;
        }
        public static double CompareBitmaps(RenderTargetBitmap bmp1, RenderTargetBitmap bmp2)
        {
            // Convert to format we can read pixels from
            var bitmap1 = ConvertToWriteableBitmap(bmp1);
            var bitmap2 = ConvertToWriteableBitmap(bmp2);

            if (bitmap1.PixelWidth != bitmap2.PixelWidth ||
                bitmap1.PixelHeight != bitmap2.PixelHeight)
                return 0; // or throw exception

            int width = bitmap1.PixelWidth;
            int height = bitmap1.PixelHeight;
            int bytesPerPixel = (bitmap1.Format.BitsPerPixel + 7) / 8;
            int totalPixels = width * height;
            int matchingPixels = 0;

            // Copy pixel data
            byte[] pixels1 = new byte[width * height * bytesPerPixel];
            byte[] pixels2 = new byte[width * height * bytesPerPixel];
            bitmap1.CopyPixels(pixels1, width * bytesPerPixel, 0);
            bitmap2.CopyPixels(pixels2, width * bytesPerPixel, 0);

            // Compare pixels
            for (int i = 0; i < pixels1.Length; i += bytesPerPixel)
            {
                bool match = true;
                for (int b = 0; b < bytesPerPixel; b++)
                {
                    if (pixels1[i + b] != pixels2[i + b])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) matchingPixels++;
            }

            return (double)matchingPixels / totalPixels;
        }

        private static WriteableBitmap ConvertToWriteableBitmap(RenderTargetBitmap renderTarget)
        {
            return new WriteableBitmap(renderTarget);
        }

        public UTCSplashScreen()
        {
            Random r = new Random();
            InitializeComponent();

            //Just for fun - today icon
            /*var none = RenderCharacterToBitmap('\uf512');
            var day = DateTime.Now.DayOfYear;
            var ch = symbols[day];
            var chimage = RenderCharacterToBitmap(ch);
            while (CompareBitmaps(none, chimage) >= 0.95)
            {
                ch = symbols[day++];
                chimage = RenderCharacterToBitmap(ch);
            }*/

            Build.Text = string.Format(CultureInfo.CurrentCulture,
                Localization.LocalizationManager.Instance["SplashScreen.BuildInfo.Template"],
                MainViewModel.GetBuildDateTime(Assembly.GetExecutingAssembly()),
                Localization.LocalizationManager.Instance["SplashScreen.BuildInfo.HttpClientVersion"]);
            
            //Icon.Text = new string(ch, 1);
            //this.RenderSize = new System.Windows.Size(400, 225);
        }
    }
}
