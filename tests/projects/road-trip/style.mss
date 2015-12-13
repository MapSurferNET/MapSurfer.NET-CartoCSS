/***********************************************************************

'Road Trip'
===========

A map of the United States inspired by the impossible-to-fold maps in
your glovebox.

***********************************************************************/

@land: #fff1cf;
@water: #C0E0F8;
@waterline: #8CE;
@water-color: #b5d0d0;

Map {
  background-color:@water;
}

.water-lines {
  [waterway = 'canal'][zoom >= 12],
  [waterway = 'river'][zoom >= 12],
  [waterway = 'wadi'][zoom >= 13] {
    [bridge = 'yes'] {
      [zoom >= 14] {
        bridgecasing/line-color: black;
        bridgecasing/line-join: round;
        bridgecasing/line-width: 6;
        [zoom >= 15] { bridgecasing/line-width: 7; }
        [zoom >= 17] { bridgecasing/line-width: 11; }
        [zoom >= 18] { bridgecasing/line-width: 13; }
      }
    }
    [intermittent = 'yes'],
    [waterway = 'wadi'] {
      [bridge = 'yes'][zoom >= 14] {
        bridgefill/line-color: white;
        bridgefill/line-join: round;
        bridgefill/line-width: 4;
        [zoom >= 15] { bridgefill/line-width: 5; }
        [zoom >= 17] { bridgefill/line-width: 9; }
        [zoom >= 18] { bridgefill/line-width: 11; }
      }
      line-dasharray: 4,3;
      line-cap: butt;
      line-join: round;
      line-clip: false;
    }
    line-color: @water-color;
    line-width: 2;
    [zoom >= 13] { line-width: 3; }
    [zoom >= 14] { line-width: 5; }
    [zoom >= 15] { line-width: 6; }
    [zoom >= 17] { line-width: 10; }
    [zoom >= 18] { line-width: 12; }
    line-cap: round;
    line-join: round;
    [int_tunnel = 'yes'] {
      line-dasharray: 4,2;
      line-cap: butt;
      line-join: miter;
      a/line-color: #f3f7f7;
      a/line-width: 1;
      [zoom >= 14] { a/line-width: 2; }
      [zoom >= 15] { a/line-width: 3; }
      [zoom >= 17] { a/line-width: 7; }
      [zoom >= 18] { a/line-width: 8; }
    }
  }

  [waterway = 'stream'],
  [waterway = 'ditch'],
  [waterway = 'drain'] {
    [zoom >= 13] {
      [bridge = 'yes'] {
        [zoom >= 14] {
          bridgecasing/line-color: black;
          bridgecasing/line-join: round;
          bridgecasing/line-width: 4;
          [waterway = 'stream'][zoom >= 15] { bridgecasing/line-width: 4; }
          bridgeglow/line-color: white;
          bridgeglow/line-join: round;
          bridgeglow/line-width: 3;
          [waterway = 'stream'][zoom >= 15] { bridgeglow/line-width: 3; }
        }
      }
      [intermittent = 'yes'] {
        line-dasharray: 4,3;
        line-cap: butt;
        line-join: round;
        line-clip: false;
      }
      line-width: 2;
      line-color: @water-color;
      [waterway = 'stream'][zoom >= 15] {
        line-width: 3;
      }
      [int_tunnel = 'yes'][zoom >= 15] {
        line-width: 3.5;
        [waterway = 'stream'] { line-width: 4.5; }
        line-dasharray: 4,2;
        a/line-width: 1;
        [waterway = 'stream'] { a/line-width: 2; }
        a/line-color: #f3f7f7;
      }
    }
  }

  [waterway = 'derelict_canal'][zoom >= 12] {
    line-width: 1.5;
    line-color: #b5e4d0;
    line-dasharray: 4,4;
    line-opacity: 0.5;
    line-join: round;
    line-cap: round;
    [zoom >= 13] {
      line-width: 2.5;
      line-dasharray: 4,6;
    }
    [zoom >= 14] {
      line-width: 4.5;
      line-dasharray: 4,8;
    }
  }
}

/*
#countries
{
  image-filters:invert();
  image-filters-inflate:true;
  direct-image-filters:invert();
}

#countries::outline {
  line-color:@waterline;
  line-width:1.6;
}

#countries::fill {
  polygon-fill:@land;
  polygon-gamma:0.75;
  [ADM0_A3='USA'] { 
   polygon-fill:lighten(@land, 7);
//   polygon-pattern-file: url(E:/uu/openstreetmap-carto-master/openstreetmap-carto-master/symbols/beach.png)
  } 
}

#lake[zoom>=0][ScaleRank<=2],
#lake[zoom>=1][ScaleRank=3],
#lake[zoom>=2][ScaleRank=4],
#lake[zoom>=3][ScaleRank=5],
#lake[zoom>=4][ScaleRank=6],
#lake[zoom>=5][ScaleRank=7],
#lake[zoom>=6][ScaleRank=8],
#lake[zoom>=7][ScaleRank=9] {
  ::outline { line-color:@waterline; }
  ::fill { polygon-fill:@water; }
}

.park { line-color:#AD9; }
.park.area { polygon-fill:#DEB; }

#country_border::glow[zoom>2] {
  line-color:#F60;
  line-opacity:0.33;
  line-width:4;
}

#country_border { line-color:#408; }
#country_border[zoom<3] { line-width:0.4; }
#country_border[zoom=3] { line-width:0.6; }
#country_border[zoom=4] { line-width:0.8; }
#country_border[zoom=5] { line-width:1.0; 
//line-pattern-file: url(D:/MapSurfer/MapData/Symbols/rail.png);
}

#country_border_marine {
  line-color:#A06;
  line-dasharray:8,2;
  line-opacity:0.3;
  line-width:0.8;
}

#state_line::glow[ADM0_A3='USA'],
#state_line::glow[ADM0_A3='CAN'] {
  [zoom>2] {
    line-color:#FD0;
    line-opacity:0.2;
    line-width:3;
  }
}
#state_line[ADM0_A3='USA'],
#state_line[ADM0_A3='CAN'] {
  [zoom>2] {
    line-dasharray:2,2,10,2;
    line-width:0.6;
  }
}
 */